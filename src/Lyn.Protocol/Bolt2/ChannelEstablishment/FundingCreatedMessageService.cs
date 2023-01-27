using System.Collections.Generic;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class FundingCreatedMessageService : IBoltMessageService<FundingCreated>
    {
        private readonly ILogger<FundingCreatedMessageService> _logger;
        private readonly IChannelCandidateRepository _channelCandidateRepository;
        private readonly IPeerRepository _peerRepository;
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly ILightningScripts _lightningScripts;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly ISecretStore _secretStore;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly ICommitmentTransactionBuilder _transactionBuilder;

        public FundingCreatedMessageService(ILogger<FundingCreatedMessageService> logger, 
            IChannelCandidateRepository channelCandidateRepository, IPeerRepository peerRepository, 
            IChainConfigProvider chainConfigProvider, ILightningScripts lightningScripts, 
            ILightningTransactions lightningTransactions, ISecretStore secretStore, 
            ILightningKeyDerivation lightningKeyDerivation, ICommitmentTransactionBuilder transactionBuilder)
        {
            _logger = logger;
            _channelCandidateRepository = channelCandidateRepository;
            _peerRepository = peerRepository;
            _chainConfigProvider = chainConfigProvider;
            _lightningScripts = lightningScripts;
            _lightningTransactions = lightningTransactions;
            _secretStore = secretStore;
            _lightningKeyDerivation = lightningKeyDerivation;
            _transactionBuilder = transactionBuilder;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<FundingCreated> message)
        {
            FundingCreated fundingCreated = message.MessagePayload;

            var channelCandidate = await _channelCandidateRepository.GetAsync(fundingCreated.TemporaryChannelId);

            if (channelCandidate == null)
            {
                return new ErrorCloseChannelResponse(fundingCreated.TemporaryChannelId, "open channel is in an invalid state");
            }

            var peer = await _peerRepository.TryGetPeerAsync(message.NodeId);

            if (peer == null)
            {
                return new ErrorCloseChannelResponse(fundingCreated.TemporaryChannelId, "invalid peer");
            }

            var chainParameters = _chainConfigProvider.GetConfiguration(channelCandidate.OpenChannel.ChainHash);

            if (chainParameters == null)
            {
                return new ErrorCloseChannelResponse(fundingCreated.TemporaryChannelId,  "chainhash is unknowen");
            }

            // david: this params can go in channelchandidate
            var optionAnchorOutputs = peer.MutuallySupportedFeature(Features.OptionAnchorOutputs);
            var optionStaticRemoteKey = peer.MutuallySupportedFeature(Features.OptionStaticRemotekey);

            var fundingOutPoint = new OutPoint
            {
                Hash = fundingCreated.FundingTxid,
                Index = (uint)fundingCreated.FundingOutputIndex
            };

            _transactionBuilder.WithAcceptChannel(channelCandidate.AcceptChannel)
                .WithOpenChannel(channelCandidate.OpenChannel)
                .WithFundingOutpoint(fundingOutPoint)
                .WithFundingSide(channelCandidate.ChannelOpener);

            if (optionAnchorOutputs) _transactionBuilder.WithAnchorOutputs();
            if (optionStaticRemoteKey) _transactionBuilder.WithStaticRemoteKey();

            var localCommitmentTransaction = _transactionBuilder.BuildLocalCommitmentTransaction();
            var remoteCommitmentTransaction = _transactionBuilder.BuildRemoteCommitmentTransaction();
            
            var fundingWscript = _lightningScripts.FundingRedeemScript(channelCandidate.OpenChannel.FundingPubkey, channelCandidate.AcceptChannel.FundingPubkey);

            if (!_lightningTransactions.VerifySignature(localCommitmentTransaction.Transaction,
                    channelCandidate.OpenChannel.FundingPubkey,
                    0,
                    fundingWscript,
                    channelCandidate.OpenChannel.FundingSatoshis,
                    _lightningTransactions.FromCompressedSignature(fundingCreated.Signature),
                    optionAnchorOutputs))
            {
                _logger.Log(LogLevel.Information,"Funding created signature failed validation");
                return new ErrorCloseChannelResponse(
                    _lightningKeyDerivation.DeriveChannelId(fundingCreated.FundingTxid,
                        (ushort)fundingCreated.FundingOutputIndex),
                    "Funding created signature failed validation");
            }

            Secret seed = _secretStore.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);
            var localSignature = _lightningTransactions.SignInput(remoteCommitmentTransaction.Transaction,
                secrets.FundingPrivkey,
                0,
                fundingWscript,
                channelCandidate.OpenChannel.FundingSatoshis,
                optionAnchorOutputs);

            var fundingSigned = new FundingSigned
            {
                ChannelId = _lightningKeyDerivation.DeriveChannelId(fundingCreated.FundingTxid, (ushort)fundingCreated.FundingOutputIndex),
                Signature = _lightningTransactions.ToCompressedSignature(localSignature)
            };
            
            channelCandidate.FundingCreated = fundingCreated;
            channelCandidate.FundingSignedLocal = fundingSigned;
            await _channelCandidateRepository.UpdateAsync(channelCandidate);
            await _channelCandidateRepository.UpdateChannelIdAsync(fundingCreated.TemporaryChannelId,
                fundingSigned.ChannelId);

            return new SuccessWithOutputResponse(new[] { new BoltMessage { Payload = fundingSigned } });
        }
        //
        //
        // //TODO David need to refactor the method from this service and the others to one service that can do both types of transactions
        // private CommitmenTransactionOut CommitmentTransactionOut(ChannelCandidate? channelCandidate, OutPoint inPoint, bool optionAnchorOutputs, bool optionStaticRemotekey)
        // {
        //     // generate the commitment transaction how it will look like for the other side
        //
        //     var commitmentTransactionIn = new CommitmentTransactionIn
        //     {
        //         Funding = channelCandidate.OpenChannel.FundingSatoshis,
        //         Htlcs = new List<Htlc>(),
        //         Opener = channelCandidate.ChannelOpener,
        //         Side = ChannelSide.Remote,
        //         CommitmentNumber = 0,
        //         FundingTxout = inPoint,
        //         DustLimitSatoshis = channelCandidate.OpenChannel.DustLimitSatoshis,
        //         FeeratePerKw = channelCandidate.OpenChannel.FeeratePerKw,
        //         LocalFundingKey = channelCandidate.OpenChannel.FundingPubkey,
        //         OptionAnchorOutputs = optionAnchorOutputs,
        //         OtherPayMsat = ((MiliSatoshis)channelCandidate.OpenChannel.FundingSatoshis) - channelCandidate.OpenChannel.PushMsat,
        //         RemoteFundingKey = channelCandidate.AcceptChannel.FundingPubkey,
        //         SelfPayMsat = channelCandidate.OpenChannel.PushMsat,
        //         ToSelfDelay = channelCandidate.OpenChannel.ToSelfDelay,
        //         CnObscurer = _lightningScripts.CommitNumberObscurer(channelCandidate.OpenChannel.PaymentBasepoint,
        //             channelCandidate.AcceptChannel.PaymentBasepoint)
        //     };
        //
        //     Basepoints localBasepoints = channelCandidate.OpenChannel.GetBasePoints();
        //     
        //     _logger.LogDebug("{@localBasepoints}", localBasepoints);
        //
        //     Basepoints remoteBasepoints = channelCandidate.AcceptChannel.GetBasePoints();
        //
        //     _logger.LogDebug("{@remoteBasepoints}", remoteBasepoints);
        //
        //     PublicKey perCommitmentPoint = channelCandidate.OpenChannel.FirstPerCommitmentPoint;
        //
        //     commitmentTransactionIn.Keyset = SetKeys(localBasepoints, remoteBasepoints, perCommitmentPoint,
        //         optionStaticRemotekey);
        //
        //     return _lightningTransactions.CommitmentTransaction(commitmentTransactionIn);
        // }
        //
        // private CommitmenTransactionOut LocalCommitmentTransactionOut(OpenChannel openChannel, AcceptChannel acceptChannel, 
        //     ChannelSide side , OutPoint inPoint, bool optionAnchorOutputs, bool optionStaticRemoteKey)
        // {
        //     // generate the commitment transaction how it will look like for the other side
        //
        //     var commitmentTransactionIn = new CommitmentTransactionIn
        //     {
        //         Funding = openChannel.FundingSatoshis,
        //         Htlcs = new List<Htlc>(),
        //         Opener = side,
        //         Side = ChannelSide.Local,
        //         CommitmentNumber = 0,
        //         FundingTxout = inPoint,
        //         DustLimitSatoshis = acceptChannel.DustLimitSatoshis,
        //         FeeratePerKw = openChannel.FeeratePerKw,
        //         LocalFundingKey = acceptChannel.FundingPubkey,
        //         RemoteFundingKey = openChannel.FundingPubkey,
        //         OptionAnchorOutputs = optionAnchorOutputs,
        //         OtherPayMsat = openChannel.PushMsat,
        //         SelfPayMsat = ((MiliSatoshis)openChannel.FundingSatoshis) - openChannel.PushMsat,
        //         ToSelfDelay = acceptChannel.ToSelfDelay,
        //         CnObscurer = _lightningScripts.CommitNumberObscurer(
        //             openChannel.PaymentBasepoint,
        //             acceptChannel.PaymentBasepoint)
        //     };
        //
        //     var localBasePoints = acceptChannel.GetBasePoints();
        //
        //     _logger.LogDebug("{@localBasePoints}", localBasePoints);
        //
        //     var remoteBasePoints = openChannel.GetBasePoints();
        //
        //     _logger.LogDebug("{@remoteBasePoints}", remoteBasePoints);
        //     
        //     commitmentTransactionIn.Keyset = SetKeys(localBasePoints, remoteBasePoints, acceptChannel.FirstPerCommitmentPoint,
        //         optionStaticRemoteKey);
        //
        //     return _lightningTransactions.CommitmentTransaction(commitmentTransactionIn);
        // }
        //
        // private Keyset SetKeys(Basepoints localBasepoints, Basepoints remoteBasepoints, PublicKey perCommitmentPoint, bool optionStaticRemotekey)
        // {
        //     var remoteRevocationKey = _lightningKeyDerivation.DeriveRevocationPublicKey(remoteBasepoints.Revocation, perCommitmentPoint);
        //
        //     var localDelayedPaymentKey = _lightningKeyDerivation.DerivePublickey(localBasepoints.DelayedPayment, perCommitmentPoint);
        //
        //     var remotePaymentKey = optionStaticRemotekey ?
        //         remoteBasepoints.Payment :
        //         _lightningKeyDerivation.DerivePublickey(remoteBasepoints.Payment, perCommitmentPoint);
        //
        //     var remoteHtlckey = _lightningKeyDerivation.DerivePublickey(remoteBasepoints.Htlc, perCommitmentPoint);
        //     var localHtlckey = _lightningKeyDerivation.DerivePublickey(localBasepoints.Htlc, perCommitmentPoint);
        //
        //     Keyset keyset = new Keyset(
        //         remoteRevocationKey,
        //         localHtlckey,
        //         remoteHtlckey,
        //         localDelayedPaymentKey,
        //         remotePaymentKey);
        //
        //     _logger.LogDebug("{@keyset}", keyset);
        //
        //     return keyset;
        // }
    }
}