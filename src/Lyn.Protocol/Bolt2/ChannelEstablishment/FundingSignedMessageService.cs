using System.Buffers;
using System.Collections.Generic;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
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
using Lyn.Types;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
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

        public FundingSignedMessageService(ILogger<FundingSignedMessageService> logger,
            ILightningTransactions lightningTransactions,
            ITransactionHashCalculator transactionHashCalculator,
            ILightningScripts lightningScripts,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelCandidateRepository,
            IChainConfigProvider chainConfigProvider,
            ISecretStore secretStore,
            IPeerRepository peerRepository)
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
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<FundingSigned> message)
        {
            FundingSigned fundingSigned = message.MessagePayload;

            ChannelCandidate? channelCandidate = await _channelCandidateRepository.GetAsync(message.MessagePayload.ChannelId);

            if (channelCandidate == null)
            {
                return MessageProcessingOutput.CreateErrorMessage(fundingSigned.ChannelId, true, "open channel is in an invalid state");
            }

            var peer = _peerRepository.TryGetPeerAsync(message.NodeId);

            if (peer == null)
            {
                return MessageProcessingOutput.CreateErrorMessage(fundingSigned.ChannelId, true, "invalid peer");
            }

            ChainParameters? chainParameters = _chainConfigProvider.GetConfiguration(channelCandidate.OpenChannel.ChainHash);

            if (chainParameters == null)
            {
                return MessageProcessingOutput.CreateErrorMessage(fundingSigned.ChannelId, true, "chainhash is unknowen");
            }

            var remotesig = _lightningTransactions.FromCompressedSignature(fundingSigned.Signature);

            _logger.LogDebug("FundingSigned - signature = {remotesig}", remotesig);

            // david: this params can go in channelchandidate
            bool optionAnchorOutputs = false;// (peer.Featurs & Features.OptionAnchorOutputs) != 0;
            bool optionStaticRemotekey = true; //(peer.Featurs & Features.OptionStaticRemotekey) != 0; ;

            Secret seed = _secretStore.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            var fundingOutPoint = new OutPoint { Hash = channelCandidate.FundingCreated.FundingTxid, Index = (uint)channelCandidate.FundingCreated.FundingOutputIndex };

            var localCommitmentTransaction = CommitmenTransactionOut(channelCandidate, secrets, fundingOutPoint, optionAnchorOutputs, optionStaticRemotekey);

            byte[]? fundingWscript = _lightningScripts.FundingRedeemScript(channelCandidate.OpenChannel.FundingPubkey, channelCandidate.AcceptChannel.FundingPubkey);

            var localFundingSign = _lightningTransactions.SignInput(
                localCommitmentTransaction.Transaction,
                secrets.FundingPrivkey,
                0,
                fundingWscript,
                channelCandidate.OpenChannel.FundingSatoshis,
                optionAnchorOutputs);

            _lightningScripts.SetCommitmentInputWitness(localCommitmentTransaction.Transaction.Inputs[0], localFundingSign, remotesig, fundingWscript);

            // VALIDATE THE TRANSACTION SIGNATURES

            var ci = new ServiceCollection().AddSerializationComponents().BuildServiceProvider();
            var serializationFactory = new SerializationFactory(ci);
            var trxhex = serializationFactory.Serialize(localCommitmentTransaction.Transaction);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("LocalCommitmentTransaction = {trxhex}", Hex.ToString(trxhex));
            }

            NBitcoin.Transaction? trx = NBitcoin.Network.Main.CreateTransaction();
            trx.FromBytes(trxhex);

            NBitcoin.TransactionBuilder builder = NBitcoin.Network.RegTest.CreateTransactionBuilder();
            var errors = builder.Check(trx);

            // for now we cant valiodate so we return erro and the trx itself, this will close the channel
            _logger.LogDebug("Failing channel {ChannelId} for Invalid Signature", fundingSigned.ChannelId);
            return MessageProcessingOutput.CreateErrorMessage(fundingSigned.ChannelId, true, $"Invalid Signature, LocalCommitmentTransaction = {Hex.ToString(trxhex)}");

            // return new MessageProcessingOutput { Success = true };
        }

        private CommitmenTransactionOut CommitmenTransactionOut(ChannelCandidate? channelCandidate, Secrets secrets, OutPoint inPoint, bool optionAnchorOutputs, bool optionStaticRemotekey)
        {
            // generate the commitment transaction how it will look like for the other side

            var commitmentTransactionIn = new CommitmentTransactionIn
            {
                Funding = channelCandidate.OpenChannel.FundingSatoshis,
                Htlcs = new List<Htlc>(),
                Opener = channelCandidate.ChannelOpener,
                Side = ChannelSide.Local,
                CommitmentNumber = 0,
                FundingTxout = inPoint,
                DustLimitSatoshis = channelCandidate.OpenChannel.DustLimitSatoshis,
                FeeratePerKw = channelCandidate.OpenChannel.FeeratePerKw,
                LocalFundingKey = channelCandidate.OpenChannel.FundingPubkey,
                OptionAnchorOutputs = optionAnchorOutputs,
                OtherPayMsat = channelCandidate.OpenChannel.PushMsat,
                RemoteFundingKey = channelCandidate.OpenChannel.FundingPubkey,
                SelfPayMsat = ((MiliSatoshis)channelCandidate.OpenChannel.FundingSatoshis) - channelCandidate.OpenChannel.PushMsat,
                ToSelfDelay = channelCandidate.OpenChannel.ToSelfDelay,
                CnObscurer = _lightningScripts.CommitNumberObscurer(
                    channelCandidate.OpenChannel.PaymentBasepoint,
                    channelCandidate.AcceptChannel.PaymentBasepoint)
            };

            Basepoints localBasepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);

            _logger.LogDebug("{@localBasepoints}", localBasepoints);

            Basepoints remoteBasepoints = new Basepoints
            {
                DelayedPayment = channelCandidate.AcceptChannel.DelayedPaymentBasepoint,
                Htlc = channelCandidate.AcceptChannel.HtlcBasepoint,
                Payment = channelCandidate.AcceptChannel.PaymentBasepoint,
                Revocation = channelCandidate.AcceptChannel.RevocationBasepoint
            };

            _logger.LogDebug("{@remoteBasepoints}", remoteBasepoints);

            PublicKey perCommitmentPoint = channelCandidate.OpenChannel.FirstPerCommitmentPoint;

            SetKeys(commitmentTransactionIn, localBasepoints, remoteBasepoints, perCommitmentPoint, optionStaticRemotekey);

            return _lightningTransactions.CommitmentTransaction(commitmentTransactionIn);
        }

        private void SetKeys(CommitmentTransactionIn transaction, Basepoints localBasepoints, Basepoints remoteBasepoints, PublicKey perCommitmentPoint, bool optionStaticRemotekey)
        {
            var remoteRevocationKey = _lightningKeyDerivation.DeriveRevocationPublicKey(remoteBasepoints.Revocation, perCommitmentPoint);

            var localDelayedPaymentKey = _lightningKeyDerivation.DerivePublickey(localBasepoints.DelayedPayment, perCommitmentPoint);

            var localPaymentKey = _lightningKeyDerivation.DerivePublickey(localBasepoints.Payment, perCommitmentPoint);

            var remotePaymentKey = optionStaticRemotekey ?
                remoteBasepoints.Payment :
                _lightningKeyDerivation.DerivePublickey(remoteBasepoints.Payment, perCommitmentPoint);

            var remoteHtlckey = _lightningKeyDerivation.DerivePublickey(remoteBasepoints.Htlc, perCommitmentPoint);
            var localHtlckey = _lightningKeyDerivation.DerivePublickey(localBasepoints.Htlc, perCommitmentPoint);

            Keyset keyset = new Keyset(
                remoteRevocationKey,
                localHtlckey,
                remoteHtlckey,
                localDelayedPaymentKey,
                localPaymentKey,
                remotePaymentKey);

            _logger.LogDebug("{@keyset}", keyset);

            transaction.Keyset = keyset;
        }
    }
}