using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Bolt2.Wallet;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Bolt7;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Hashing;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class FundingLockedMessageService : IBoltMessageService<FundingLocked>
    {
        private readonly ILogger<FundingLockedMessageService> _logger;
        private readonly IChannelCandidateRepository _channelCandidateRepository;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly IPeerRepository _peerRepository;
        private readonly ISecretStore _secretStore;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IGossipRepository _gossipRepository;
        private readonly IParseFeatureFlags _featureFlags;
        private readonly INodeSettings _nodeSettings;
        private readonly IWalletTransactions _walletTransactions;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly ISerializationFactory _serializationFactory;

        public FundingLockedMessageService(ILogger<FundingLockedMessageService> logger, 
            IChannelCandidateRepository channelCandidateRepository, IPaymentChannelRepository paymentChannelRepository, 
            ISecretStore secretStore, ILightningKeyDerivation lightningKeyDerivation, IGossipRepository gossipRepository, 
            IPeerRepository peerRepository, IParseFeatureFlags featureFlags, INodeSettings nodeSettings, IWalletTransactions walletTransactions, ILightningTransactions lightningTransactions, ISerializationFactory serializationFactory)
        {
            _logger = logger;
            _channelCandidateRepository = channelCandidateRepository;
            _paymentChannelRepository = paymentChannelRepository;
            _secretStore = secretStore;
            _lightningKeyDerivation = lightningKeyDerivation;
            _gossipRepository = gossipRepository;
            _peerRepository = peerRepository;
            _featureFlags = featureFlags;
            _nodeSettings = nodeSettings;
            _walletTransactions = walletTransactions;
            _lightningTransactions = lightningTransactions;
            _serializationFactory = serializationFactory;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<FundingLocked> message)
        {
            _logger.LogDebug("Processing funding locked from peer");

            var existingChannel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(message.MessagePayload.ChannelId); //TODO validate the message before using it

            if (existingChannel != null)
            {
                // this was a reconnection process for a new channel can just ignore the message
                return new EmptySuccessResponse();
            }
            
            var channelCandidate = await _channelCandidateRepository.GetAsync(message.MessagePayload.ChannelId);

            if (channelCandidate == null)
            {
                _logger.LogWarning($"Channel candidate not found in the repository for channel id {message.MessagePayload.ChannelId}");
                
                return new ErrorCloseChannelResponse(message.MessagePayload.ChannelId,
                    "open channel is in an invalid state");
            }

            var peer = await _peerRepository.TryGetPeerAsync(message.NodeId)
                ?? throw new ArgumentException(nameof(message.NodeId));

            channelCandidate.FundingLocked = message.MessagePayload;

            if (channelCandidate.ChannelOpener !=  ChannelSide.Local) // We will be publishing to block chain in that case
            {
                _logger.LogDebug("Confirmation of funding transaction not received yet");
                
                await _channelCandidateRepository.UpdateAsync(channelCandidate); //Waiting for confirmation from our side as well

                var fundingTransaction = await _walletTransactions.GetTransactionByIdAsync(channelCandidate.FundingCreated.FundingTxid);

                if (fundingTransaction == null) //We still didn't see the transaction on the blockchain
                    return new EmptySuccessResponse();
            }

            var shortChannelId = await _walletTransactions.LookupShortChannelIdByTransactionHashAsync(
                channelCandidate.FundingCreated?.FundingTxid,
                channelCandidate.FundingCreated.FundingOutputIndex.Value);
            
            //Time to create the payment channel
            PaymentChannel paymentChannel = BuildPaymentChannel(message.MessagePayload.NextPerCommitmentPoint,
                channelCandidate, shortChannelId);

            await _paymentChannelRepository.AddNewPaymentChannelAsync(paymentChannel);
            
            _logger.LogDebug("Payment channel created");
            
            var seed = _secretStore.GetSeed();
            var secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            await AddChannelToGossipRepository(message.NodeId, paymentChannel, peer,
                channelCandidate.OpenChannel.ChainHash, secrets);
            
            var fundingLockedResponse = new FundingLocked
            {
                ChannelId = channelCandidate.FundingLocked.ChannelId,
                NextPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed, paymentChannel.LocalCommitmentNumber + 1)
            };
            
            _logger.LogDebug("Replaying with funding locked ");

            return new SuccessWithOutputResponse(new BoltMessage { Payload = fundingLockedResponse });
        }

        private async Task AddChannelToGossipRepository(PublicKey remoteNodeId,PaymentChannel paymentChannel, Peer peer,
            UInt256 chainHash, Secrets secrets)
        {
            var nodeIds = new List<byte[]>
            {
                _nodeSettings.GetNodeId(),
                remoteNodeId
            };

            nodeIds.Sort(new LexicographicByteComparer());
            
            var isLocalNode1 = nodeIds.First().SequenceEqual((byte[])_nodeSettings.GetNodeId());

            var gossipChannel = new GossipChannel(
                new ChannelAnnouncement
                {
                    ShortChannelId = paymentChannel.ShortChannelId,
                    Features = _featureFlags.ParseFeatures(peer.MutuallySupportedFeatures),
                    ChainHash = chainHash,
                    NodeId1 = nodeIds.First(),
                    BitcoinKey1 = isLocalNode1 ? paymentChannel.LocalFundingKey : paymentChannel.RemoteFundingKey,
                    BitcoinKey2 = isLocalNode1 ? paymentChannel.RemoteFundingKey : paymentChannel.LocalFundingKey,
                    NodeId2 = nodeIds.Last()
                },
                isLocalNode1 ? GossipChannel.LocalNode.Node1 : GossipChannel.LocalNode.Node2);

            var serializedChannelAnnouncement = _serializationFactory.Serialize(gossipChannel.ChannelAnnouncement
                .GetChannelWithoutSignatures()); 
            
            var hash = new UInt256(HashGenerator.DoubleSha256(serializedChannelAnnouncement)); 
            
            var localNodeSignature = _lightningTransactions.SignByteArray(hash, secrets.FundingPrivkey);
            var localBitcoinSignature = _lightningTransactions.SignByteArray(hash, _nodeSettings.GetNodePrivateKey());

            if (isLocalNode1)
            {
                gossipChannel.ChannelAnnouncement.NodeSignature1 = localNodeSignature;
                gossipChannel.ChannelAnnouncement.BitcoinSignature1 = localBitcoinSignature;
            }
            else
            {
                gossipChannel.ChannelAnnouncement.NodeSignature2 = localNodeSignature;
                gossipChannel.ChannelAnnouncement.BitcoinSignature2 = localBitcoinSignature;
            }
            
            await _gossipRepository.AddGossipChannelAsync(gossipChannel);
        }

        private static PaymentChannel BuildPaymentChannel(PublicKey nextPerCommitmentPoint, ChannelCandidate channelCandidate, ShortChannelId shortChannelId)
        {
            return new PaymentChannel(
                channelCandidate.ChannelId ?? throw new InvalidOperationException(),
                shortChannelId ?? throw new InvalidOperationException(),
                channelCandidate.ChannelOpener == ChannelSide.Local
                    ? channelCandidate.FundingSignedRemote?.Signature ?? throw new InvalidOperationException()
                    : channelCandidate.FundingCreated?.Signature ?? throw new InvalidOperationException(),
                nextPerCommitmentPoint ?? throw new InvalidOperationException(),
                new[]
                {
                    channelCandidate.AcceptChannel?.FirstPerCommitmentPoint ?? throw new InvalidOperationException()
                },
                channelCandidate.OpenChannel?.FundingSatoshis ?? throw new InvalidOperationException(),
                new OutPoint
                {
                    Hash = channelCandidate.FundingCreated?.FundingTxid ?? throw new InvalidOperationException(),
                    Index = channelCandidate.FundingCreated.FundingOutputIndex ?? throw new InvalidOperationException()
                },
                channelCandidate.OpenChannel.DustLimitSatoshis,
                channelCandidate.AcceptChannel.DustLimitSatoshis ?? throw new InvalidOperationException(),
                channelCandidate.OpenChannel.FeeratePerKw,
                channelCandidate.OpenChannel.FundingPubkey,
                channelCandidate.AcceptChannel.FundingPubkey ?? throw new InvalidOperationException(),
                channelCandidate.OpenChannel.PushMsat,
                channelCandidate.OpenChannel.GetBasePoints(),
                channelCandidate.AcceptChannel.GetBasePoints(),
                channelCandidate.ChannelOpener,
                channelCandidate.OpenChannel.ToSelfDelay,
                channelCandidate.AcceptChannel.ToSelfDelay);
        }
    }
}