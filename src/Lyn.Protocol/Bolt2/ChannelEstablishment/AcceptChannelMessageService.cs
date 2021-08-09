using System;
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
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt9;
using Lyn.Types;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.DependencyInjection;

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
        private readonly IPeerRepository _peerRepository;
        private readonly IBoltFeatures _boltFeatures;

        public AcceptChannelMessageService(ILogger<AcceptChannelMessageService> logger,
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

            var peer = _peerRepository.TryGetPeerAsync(message.NodeId);

            if (peer == null)
            {
                return MessageProcessingOutput.CreateErrorMessage(acceptChannel.TemporaryChannelId, true, "invalid peer");
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

            var fundingScript = _lightningScripts.FundingWitnessScript(channelCandidate.OpenChannel.FundingPubkey, channelCandidate.AcceptChannel.FundingPubkey);

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

            var fundingTransactionHash = _transactionHashCalculator.ComputeHash(fundingTransaction);
            uint fundingTransactionIndex = 0;

            // david: this params can go in channelchandidate
            bool optionAnchorOutputs = (_boltFeatures.SupportedFeatures & peer.Featurs & Features.OptionAnchorOutputs) != 0;
            bool optionStaticRemotekey = (_boltFeatures.SupportedFeatures & peer.Featurs & Features.OptionStaticRemotekey) != 0; // not sure why this must be on if other side supports it and we don't

            Secret seed = _secretStore.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            var fundingOutPoint = new OutPoint { Hash = fundingTransactionHash, Index = fundingTransactionIndex };

            var remoteCommitmentTransaction = CommitmentTransactionOut(channelCandidate, secrets, fundingOutPoint, optionAnchorOutputs, optionStaticRemotekey);

            byte[]? fundingWscript = _lightningScripts.FundingRedeemScript(channelCandidate.OpenChannel.FundingPubkey, channelCandidate.AcceptChannel.FundingPubkey);

            var remoteFundingSign = _lightningTransactions.SignInput(
                remoteCommitmentTransaction.Transaction,
                secrets.FundingPrivkey,
                0,
                fundingWscript,
                channelCandidate.OpenChannel.FundingSatoshis,
                optionAnchorOutputs);

            _lightningScripts.SetCommitmentInputWitness(remoteCommitmentTransaction.Transaction.Inputs[0], remoteFundingSign, new BitcoinSignature(new byte[74]), fundingWscript);

            channelCandidate.RemoteCommitmentTransaction = remoteCommitmentTransaction.Transaction;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                // todo: dan inject SerializationFactory
                var ci = new ServiceCollection().AddSerializationComponents().BuildServiceProvider();
                var serializationFactory = new SerializationFactory(ci);

                var trxhex = serializationFactory.Serialize(channelCandidate.RemoteCommitmentTransaction);
                _logger.LogDebug("RemoteCommitmentTransaction = {trxhex}", Hex.ToString(trxhex));
            }

            _logger.LogDebug("Remote Commitment signature = {remoteFundingSign}", remoteFundingSign);

            UInt256 newChannelId = _lightningKeyDerivation.DeriveChannelId(fundingTransactionHash, (ushort)fundingTransactionIndex);

            _logger.LogDebug("New channelid = {newChannelId}", newChannelId);

            var fundingCreated = new FundingCreated
            {
                FundingTxid = fundingTransactionHash,
                FundingOutputIndex = (ushort)fundingTransactionIndex,
                TemporaryChannelId = acceptChannel.TemporaryChannelId,
                Signature = _lightningTransactions.ToCompressedSignature(remoteFundingSign)
            };

            channelCandidate.FundingCreated = fundingCreated;

            // todo: David refactor- this go in to one method
            await _channelCandidateRepository.UpdateAsync(channelCandidate);
            await _channelCandidateRepository.UpdateChannelIdAsync(channelCandidate.ChannelId, newChannelId);

            var boltMessage = new BoltMessage
            {
                Payload = fundingCreated,
            };

            return new MessageProcessingOutput { Success = true, ResponseMessages = new[] { boltMessage } };
        }

        private CommitmenTransactionOut CommitmentTransactionOut(ChannelCandidate? channelCandidate, Secrets secrets, OutPoint inPoint, bool optionAnchorOutputs, bool optionStaticRemotekey)
        {
            // generate the commitment transaction how it will look like for the other side

            var commitmentTransactionIn = new CommitmentTransactionIn
            {
                Funding = channelCandidate.OpenChannel.FundingSatoshis,
                Htlcs = new List<Htlc>(),
                Opener = channelCandidate.ChannelOpener,
                Side = ChannelSide.Remote,
                CommitmentNumber = 0,
                FundingTxout = inPoint,
                DustLimitSatoshis = channelCandidate.AcceptChannel.DustLimitSatoshis,
                FeeratePerKw = channelCandidate.OpenChannel.FeeratePerKw,
                LocalFundingKey = channelCandidate.AcceptChannel.FundingPubkey,
                OptionAnchorOutputs = optionAnchorOutputs,
                OtherPayMsat = ((MiliSatoshis)channelCandidate.OpenChannel.FundingSatoshis) - channelCandidate.OpenChannel.PushMsat,
                RemoteFundingKey = channelCandidate.OpenChannel.FundingPubkey,
                SelfPayMsat = channelCandidate.OpenChannel.PushMsat,
                ToSelfDelay = channelCandidate.AcceptChannel.ToSelfDelay,
                CnObscurer = _lightningScripts.CommitNumberObscurer(
                    channelCandidate.OpenChannel.PaymentBasepoint,
                    channelCandidate.AcceptChannel.PaymentBasepoint)
            };

            Basepoints localBasepoints = channelCandidate.AcceptChannel.GetBasePoints();
            
            _logger.LogDebug("{@localBasepoints}", localBasepoints);

            Basepoints remoteBasepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);

            _logger.LogDebug("{@remoteBasepoints}", remoteBasepoints);

            PublicKey perCommitmentPoint = channelCandidate.AcceptChannel.FirstPerCommitmentPoint;

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