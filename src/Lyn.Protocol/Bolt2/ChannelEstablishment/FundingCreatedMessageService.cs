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

            var remoteCommitmentTransaction = _transactionBuilder.BuildRemoteCommitmentTransaction();
            
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
    }
}