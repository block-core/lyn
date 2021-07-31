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
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Lyn.Types;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class AcceptChannelMessageService : IBoltMessageService<AcceptChannel>
    {
        private readonly ILogger<AcceptChannelMessageService> _logger;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly ITransactionHashCalculator _transactionHashCalculator;
        private readonly ILightningScripts _lightningScripts;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IChannelCandidateRepository _channelCandidateRepository;
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly ISecretStore _secretStore;

        public AcceptChannelMessageService(ILogger<AcceptChannelMessageService> logger,
            ILightningTransactions lightningTransactions,
            ITransactionHashCalculator transactionHashCalculator,
            ILightningScripts lightningScripts,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelCandidateRepository,
            IChainConfigProvider chainConfigProvider,
            ISecretStore secretStore)
        {
            _logger = logger;
            _lightningTransactions = lightningTransactions;
            _transactionHashCalculator = transactionHashCalculator;
            _lightningScripts = lightningScripts;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelCandidateRepository = channelCandidateRepository;
            _chainConfigProvider = chainConfigProvider;
            _secretStore = secretStore;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<AcceptChannel> message)
        {
            AcceptChannel acceptChannel = message.MessagePayload;

            ChannelCandidate? channelCandidate = await _channelCandidateRepository.GetAsync(message.MessagePayload.TemporaryChannelId);

            if (channelCandidate == null)
            {
                return MessageProcessingOutput.CreateErrorMessage(acceptChannel.TemporaryChannelId, true, "open channel is in an invalid state");
            }

            if (channelCandidate.ChannelOpener == ChannelSide.Local
                && channelCandidate.OpenChannel != null
                && channelCandidate.AcceptChannel == null)
            {
                // continue processing
            }
            else
            {
                return MessageProcessingOutput.CreateErrorMessage(acceptChannel.TemporaryChannelId, true, "open channel is in an invalid state");
            }

            ChainParameters? chainParameters = _chainConfigProvider.GetConfiguration(channelCandidate.OpenChannel.ChainHash);

            if (chainParameters == null)
            {
                return MessageProcessingOutput.CreateErrorMessage(acceptChannel.TemporaryChannelId, true, "chainhash is unknowen");
            }

            if (acceptChannel.MinimumDepth > chainParameters.ChannelBoundariesConfig.MinimumDepth)
            {
                return MessageProcessingOutput.CreateErrorMessage(acceptChannel.TemporaryChannelId, true, "minimum_depth is unreasonably large");
            }

            if (acceptChannel.DustLimitSatoshis > channelCandidate.OpenChannel.ChannelReserveSatoshis)
            {
                return MessageProcessingOutput.CreateErrorMessage(acceptChannel.TemporaryChannelId, true, "channel_reserve_satoshis is less than dust_limit_satoshis within the open_channel message");
            }

            if (channelCandidate.OpenChannel.DustLimitSatoshis > acceptChannel.ChannelReserveSatoshis)
            {
                return MessageProcessingOutput.CreateErrorMessage(acceptChannel.TemporaryChannelId, true, "channel_reserve_satoshis from the open_channel message is less than dust_limit_satoshis");
            }

            channelCandidate.AcceptChannel = acceptChannel;

            await _channelCandidateRepository.UpdateAsync(channelCandidate);

            var fundingScript = _lightningScripts.CreateFundingTransactionScript(channelCandidate.OpenChannel.FundingPubkey, channelCandidate.AcceptChannel.FundingPubkey);

            // todo: create the transaction with the fundingScript as output and the input will be taken and signed from a wallet interface (create a wallet interface)

            var fundingTransaction = new Transaction
            {
                Outputs = new[]
                {
                    new TransactionOutput
                    {
                        PublicKeyScript = fundingScript,
                        Value = channelCandidate.OpenChannel.FundingSatoshis
                    }
                }
            };

            var transactionHash = _transactionHashCalculator.ComputeHash(fundingTransaction);

            Secret seed = _secretStore.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            var commitmentTransaction = CommitmenTransactionOut(channelCandidate, acceptChannel, secrets,
                new OutPoint { Hash = transactionHash, Index = 0 });

            byte[]? fundingWscript = _lightningScripts.FundingRedeemScript(_lightningKeyDerivation.PublicKeyFromPrivateKey(secrets.FundingPrivkey), acceptChannel.FundingPubkey);

            var bitsign = _lightningTransactions.SignInputCompressed(commitmentTransaction.Transaction, secrets.FundingPrivkey, 0,
                fundingWscript, channelCandidate.OpenChannel.FundingSatoshis, false);

            channelCandidate.CommitmentTransaction = commitmentTransaction.Transaction;

            await _channelCandidateRepository.UpdateAsync(channelCandidate);

            var fundingCreated = new FundingCreated
            {
                FundingTxid = transactionHash,
                FundingOutputIndex = 0,
                TemporaryChannelId = acceptChannel.TemporaryChannelId,
                Signature = bitsign
            };

            var boltMessage = new BoltMessage
            {
                Payload = fundingCreated,
            };

            return new MessageProcessingOutput { Success = true, ResponseMessages = new[] { boltMessage } };
        }

        private CommitmenTransactionOut CommitmenTransactionOut(ChannelCandidate? channelCandidate, AcceptChannel acceptChannel,
            Secrets secrets, OutPoint inPoint)
        {
            var test = new CommitmentTransactionIn
            {
                Funding = channelCandidate.OpenChannel.FundingSatoshis,
                Htlcs = new List<Htlc>(),
                Opener = ChannelSide.Local,
                Side = ChannelSide.Local,
                CommitmentNumber = 0,
                FundingTxout = inPoint,
                DustLimitSatoshis = channelCandidate.OpenChannel.DustLimitSatoshis,
                FeeratePerKw = channelCandidate.OpenChannel.FeeratePerKw,
                LocalFundingKey = channelCandidate.OpenChannel.FundingPubkey,
                OptionAnchorOutputs = false,
                OtherPayMsat = 0,
                RemoteFundingKey = acceptChannel.FundingPubkey,
                SelfPayMsat = channelCandidate.OpenChannel.FundingSatoshis,
                ToSelfDelay = channelCandidate.OpenChannel.ToSelfDelay
            };

            SetKeys(test, secrets, acceptChannel, channelCandidate.OpenChannel.FirstPerCommitmentPoint);

            return _lightningTransactions.CommitmentTransaction(test);
        }

        private void SetKeys(CommitmentTransactionIn transaction, Secrets secrets, AcceptChannel channel, PublicKey perCommitmentPoint)
        {
            Basepoints basepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);

            var LocalDelayedSecretkey = _lightningKeyDerivation.DerivePrivatekey(secrets.DelayedPaymentBasepointSecret, basepoints.DelayedPayment, perCommitmentPoint);

            var RemoteRevocationKey = _lightningKeyDerivation.DeriveRevocationPublicKey(channel.RevocationBasepoint, perCommitmentPoint);

            var LocalDelayedkey = _lightningKeyDerivation.PublicKeyFromPrivateKey(LocalDelayedSecretkey);

            var Localkey = _lightningKeyDerivation.DerivePublickey(basepoints.Payment, perCommitmentPoint);

            var Remotekey = _lightningKeyDerivation.DerivePublickey(channel.PaymentBasepoint, perCommitmentPoint);

            var RemoteHtlckey = _lightningKeyDerivation.DerivePublickey(channel.HtlcBasepoint, perCommitmentPoint);

            var LocalHtlcsecretkey = _lightningKeyDerivation.DerivePrivatekey(secrets.HtlcBasepointSecret, basepoints.Payment, perCommitmentPoint);

            var LocalHtlckey = _lightningKeyDerivation.PublicKeyFromPrivateKey(LocalHtlcsecretkey);

            transaction.CnObscurer =
                _lightningScripts.CommitNumberObscurer(basepoints.Payment, channel.PaymentBasepoint);

            Keyset keyset = default;

            keyset.LocalRevocationKey = RemoteRevocationKey;
            keyset.LocalDelayedPaymentKey = LocalDelayedkey;
            keyset.LocalPaymentKey = Localkey;
            keyset.RemotePaymentKey = Remotekey;
            keyset.LocalHtlcKey = LocalHtlckey;
            keyset.RemoteHtlcKey = RemoteHtlckey;

            transaction.Keyset = keyset;
        }
    }
}