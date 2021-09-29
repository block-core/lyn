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
        private readonly ITransactionHashCalculator _transactionHashCalculator;
        private readonly ILightningScripts _lightningScripts;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IChannelCandidateRepository _channelCandidateRepository;
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly ISecretStore _secretStore;
        private readonly IPeerRepository _peerRepository;
        private readonly IBoltFeatures _boltFeatures;

        public FundingSignedMessageService(ILogger<FundingSignedMessageService> logger,
            ILightningTransactions lightningTransactions,
            ITransactionHashCalculator transactionHashCalculator,
            ILightningScripts lightningScripts,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelCandidateRepository,
            IChainConfigProvider chainConfigProvider,
            ISecretStore secretStore,
            IPeerRepository peerRepository,
            IBoltFeatures boltFeatures)
        {
            _logger = logger;
            _lightningTransactions = lightningTransactions;
            _transactionHashCalculator = transactionHashCalculator;
            _lightningScripts = lightningScripts;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelCandidateRepository = channelCandidateRepository;
            _chainConfigProvider = chainConfigProvider;
            _secretStore = secretStore;
            _peerRepository = peerRepository;
            _boltFeatures = boltFeatures;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<FundingSigned> message)
        {
            FundingSigned fundingSigned = message.MessagePayload;

            var channelCandidate = await _channelCandidateRepository.GetAsync(message.MessagePayload.ChannelId);

            if (channelCandidate == null)
            {
                return new ErrorCloseChannelResponse(fundingSigned.ChannelId, "open channel is in an invalid state");
            }

            var peer = _peerRepository.TryGetPeerAsync(message.NodeId);

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
            var optionAnchorOutputs = (_boltFeatures.SupportedFeatures & Features.OptionAnchorOutputs & peer.Featurs) != 0;
            bool optionStaticRemotekey = (_boltFeatures.SupportedFeatures & Features.OptionStaticRemotekey & peer.Featurs) != 0;

            var fundingOutPoint = new OutPoint
            {
                Hash = channelCandidate.FundingCreated.FundingTxid,
                Index = (uint)channelCandidate.FundingCreated.FundingOutputIndex
            };

            var localCommitmentTransaction = LocalCommitmentTransactionOut(channelCandidate.OpenChannel, channelCandidate.AcceptChannel,
                channelCandidate.ChannelOpener, fundingOutPoint, optionAnchorOutputs, optionStaticRemotekey);

            var fundingWscript = _lightningScripts.FundingRedeemScript(channelCandidate.OpenChannel.FundingPubkey, channelCandidate.AcceptChannel.FundingPubkey);
            
            var remoteSigValid = _lightningTransactions.VerifySignature(localCommitmentTransaction.Transaction,
                channelCandidate.AcceptChannel.FundingPubkey,
                0,
                fundingWscript,
                channelCandidate.OpenChannel.FundingSatoshis,
                remoteFundingSig,
                optionAnchorOutputs);

            if (remoteSigValid) 
                return new EmptySuccessResponse();
            
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

        private CommitmenTransactionOut LocalCommitmentTransactionOut(OpenChannel openChannel, AcceptChannel acceptChannel, 
            ChannelSide side , OutPoint inPoint, bool optionAnchorOutputs, bool optionStaticRemoteKey)
        {
            // generate the commitment transaction how it will look like for the other side

            var commitmentTransactionIn = new CommitmentTransactionIn
            {
                Funding = openChannel.FundingSatoshis,
                Htlcs = new List<Htlc>(),
                Opener = side,
                Side = ChannelSide.Local,
                CommitmentNumber = 0,
                FundingTxout = inPoint,
                DustLimitSatoshis = openChannel.DustLimitSatoshis,
                FeeratePerKw = openChannel.FeeratePerKw,
                LocalFundingKey = openChannel.FundingPubkey,
                RemoteFundingKey = acceptChannel.FundingPubkey,
                OptionAnchorOutputs = optionAnchorOutputs,
                OtherPayMsat = openChannel.PushMsat,
                SelfPayMsat = ((MiliSatoshis)openChannel.FundingSatoshis) - openChannel.PushMsat,
                ToSelfDelay = acceptChannel.ToSelfDelay,
                CnObscurer = _lightningScripts.CommitNumberObscurer(
                    openChannel.PaymentBasepoint,
                    acceptChannel.PaymentBasepoint)
            };

            var localBasePoints = openChannel.GetBasePoints();

            _logger.LogDebug("{@localBasePoints}", localBasePoints);

            var remoteBasePoints = acceptChannel.GetBasePoints();

            _logger.LogDebug("{@remoteBasePoints}", remoteBasePoints);
            
            commitmentTransactionIn.Keyset = SetKeys(localBasePoints, remoteBasePoints, openChannel.FirstPerCommitmentPoint,
                optionStaticRemoteKey);

            return _lightningTransactions.CommitmentTransaction(commitmentTransactionIn);
        }

        private Keyset SetKeys(Basepoints localBasepoints, Basepoints remoteBasepoints, PublicKey perCommitmentPoint, bool optionStaticRemotekey)
        {
            var remoteRevocationKey = _lightningKeyDerivation.DeriveRevocationPublicKey(remoteBasepoints.Revocation, perCommitmentPoint);

            var localDelayedPublicKey = _lightningKeyDerivation.DerivePublickey(localBasepoints.DelayedPayment, perCommitmentPoint);
            
            var remotePaymentKey = optionStaticRemotekey 
                ? remoteBasepoints.Payment 
                : _lightningKeyDerivation.DerivePublickey(remoteBasepoints.Payment, perCommitmentPoint);

            var remoteHtlckey = _lightningKeyDerivation.DerivePublickey(remoteBasepoints.Htlc, perCommitmentPoint);
            var localHtlckey = _lightningKeyDerivation.DerivePublickey(localBasepoints.Htlc, perCommitmentPoint);

            Keyset keyset = new (
                remoteRevocationKey,
                localHtlckey,
                remoteHtlckey,
                localDelayedPublicKey,
                remotePaymentKey);

            _logger.LogDebug("{@keyset}", keyset);

            return keyset;
        }
    }
}