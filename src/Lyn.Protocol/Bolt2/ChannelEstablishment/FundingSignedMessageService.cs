using System.Collections.Generic;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt2.Wallet;
using Lyn.Protocol.Bolt9;
using Lyn.Types;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.DependencyInjection;
using OutPoint = Lyn.Types.Bitcoin.OutPoint;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class FundingSignedMessageService : IBoltMessageService<FundingSigned>
    {
        private readonly ILogger<FundingSignedMessageService> _logger;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly ILightningScripts _lightningScripts;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IChannelCandidateRepository _channelCandidateRepository;
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly IPeerRepository _peerRepository;
        private readonly IBoltFeatures _boltFeatures;
        private readonly IWalletTransactions _walletTransactions;
        private readonly ICommitmentTransactionBuilder _transactionBuilder;

        public FundingSignedMessageService(ILogger<FundingSignedMessageService> logger,
            ILightningTransactions lightningTransactions,
            ILightningScripts lightningScripts,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelCandidateRepository,
            IChainConfigProvider chainConfigProvider,
            IPeerRepository peerRepository,
            IBoltFeatures boltFeatures, 
            IWalletTransactions walletTransactions, ICommitmentTransactionBuilder transactionBuilder)
        {
            _logger = logger;
            _lightningTransactions = lightningTransactions;
            _lightningScripts = lightningScripts;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelCandidateRepository = channelCandidateRepository;
            _chainConfigProvider = chainConfigProvider;
            _peerRepository = peerRepository;
            _boltFeatures = boltFeatures;
            _walletTransactions = walletTransactions;
            _transactionBuilder = transactionBuilder;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<FundingSigned> message)
        {
            FundingSigned fundingSigned = message.MessagePayload;

            var channelCandidate = await _channelCandidateRepository.GetAsync(message.MessagePayload.ChannelId);

            if (channelCandidate == null)
            {
                return new ErrorCloseChannelResponse(fundingSigned.ChannelId, "open channel is in an invalid state");
            }

            var peer = await _peerRepository.TryGetPeerAsync(message.NodeId);

            if (peer == null)
            {
                return new ErrorCloseChannelResponse(fundingSigned.ChannelId, "invalid peer");
            }

            var chainParameters = _chainConfigProvider.GetConfiguration(channelCandidate.OpenChannel.ChainHash);

            if (chainParameters == null)
            {
                return new ErrorCloseChannelResponse(fundingSigned.ChannelId,  "chainhash is unknowen");
            }

            var remoteFundingSig = _lightningTransactions.FromCompressedSignature(fundingSigned.Signature);

            _logger.LogDebug("FundingSigned - signature = {remotesig}", remoteFundingSig);

            // david: this params can go in channelchandidate
            var optionAnchorOutputs = peer.MutuallySupportedFeature(Features.OptionAnchorOutputs);
            var optionStaticRemoteKey = peer.MutuallySupportedFeature(Features.OptionStaticRemotekey);

            var fundingOutPoint = new OutPoint
            {
                Hash = channelCandidate.FundingCreated.FundingTxid,
                Index = (uint)channelCandidate.FundingCreated.FundingOutputIndex
            };
            
            _transactionBuilder.WithAcceptChannel(channelCandidate.AcceptChannel)
                .WithOpenChannel(channelCandidate.OpenChannel)
                .WithFundingOutpoint(fundingOutPoint)
                .WithFundingSide(channelCandidate.ChannelOpener);
            
            if (optionAnchorOutputs) _transactionBuilder.WithAnchorOutputs();
            if (optionStaticRemoteKey) _transactionBuilder.WithStaticRemoteKey();

            var localCommitmentTransaction = _transactionBuilder.BuildLocalCommitmentTransaction();

            var fundingWscript = _lightningScripts.FundingRedeemScript(channelCandidate.OpenChannel.FundingPubkey, channelCandidate.AcceptChannel.FundingPubkey);
            
            var remoteSigValid = _lightningTransactions.VerifySignature(localCommitmentTransaction.Transaction,
                channelCandidate.AcceptChannel.FundingPubkey,
                0,
                fundingWscript,
                channelCandidate.OpenChannel.FundingSatoshis,
                remoteFundingSig,
                optionAnchorOutputs);

            if (!remoteSigValid)
            {
                var ci = new ServiceCollection().AddSerializationComponents().BuildServiceProvider();
                var serializationFactory = new SerializationFactory(ci);
                var trxhex = serializationFactory.Serialize(localCommitmentTransaction.Transaction);
                
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("LocalCommitmentTransaction = {trxhex}", Hex.ToString(trxhex));
                }
                // for now we cant valiodate so we return erro and the trx itself, this will close the channel
                _logger.LogDebug("Failing channel {ChannelId} for Invalid Signature", fundingSigned.ChannelId);
                return new ErrorCloseChannelResponse(fundingSigned.ChannelId,  $"Invalid Signature, LocalCommitmentTransaction = {Hex.ToString(trxhex)}");
            }

            channelCandidate.FundingSignedRemote = fundingSigned;
            await _channelCandidateRepository.UpdateAsync(channelCandidate);
            
            await _walletTransactions.PublishTransactionAsync(channelCandidate.FundingTransaction);
            
            return new EmptySuccessResponse();
        }
    }
}