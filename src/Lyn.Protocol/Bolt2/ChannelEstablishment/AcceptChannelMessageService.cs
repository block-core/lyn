using System;
using System.Collections.Generic;
using System.Linq;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt2.Wallet;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types;
using Lyn.Types.Fundamental;
using NBitcoin;
using OutPoint = Lyn.Types.Bitcoin.OutPoint;

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
        private readonly IWalletTransactions _walletTransactions;
        private readonly ICommitmentTransactionBuilder _transactionBuilder;
        private readonly ISerializationFactory _serializationFactory;

        public AcceptChannelMessageService(ILogger<AcceptChannelMessageService> logger,
            ILightningTransactions lightningTransactions,
            ITransactionHashCalculator transactionHashCalculator,
            ILightningScripts lightningScripts,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelCandidateRepository,
            IChainConfigProvider chainConfigProvider,
            ISecretStore secretStore,
            IPeerRepository peerRepository,
            IBoltFeatures boltFeatures, 
            IWalletTransactions walletTransactions, ICommitmentTransactionBuilder transactionBuilder, ISerializationFactory serializationFactory)
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
            _walletTransactions = walletTransactions;
            _transactionBuilder = transactionBuilder;
            _serializationFactory = serializationFactory;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<AcceptChannel> message)
        {
            AcceptChannel acceptChannel = message.MessagePayload;

            ChannelCandidate? channelCandidate = await _channelCandidateRepository.GetAsync(message.MessagePayload.TemporaryChannelId);

            if (channelCandidate == null)
            {
                return new ErrorCloseChannelResponse(acceptChannel.TemporaryChannelId, "open channel is in an invalid state");
            }

            if (!(channelCandidate.ChannelOpener == ChannelSide.Local
                && channelCandidate.OpenChannel != null
                && channelCandidate.AcceptChannel == null))
            {
                return new ErrorCloseChannelResponse(acceptChannel.TemporaryChannelId, "open channel is in an invalid state");
            }

            var peer = await _peerRepository.TryGetPeerAsync(message.NodeId);

            if (peer == null)
            {
                return new ErrorCloseChannelResponse(acceptChannel.TemporaryChannelId, "invalid peer");
            }

            ChainParameters? chainParameters = _chainConfigProvider.GetConfiguration(channelCandidate.OpenChannel.ChainHash);

            if (chainParameters == null)
            {
                return new ErrorCloseChannelResponse(acceptChannel.TemporaryChannelId,  "chainhash is unknowen");
            }

            if (acceptChannel.MinimumDepth > chainParameters.ChannelBoundariesConfig.MinimumDepth)
            {
                return new ErrorCloseChannelResponse(acceptChannel.TemporaryChannelId,  "minimum_depth is unreasonably large");
            }

            if (acceptChannel.DustLimitSatoshis > channelCandidate.OpenChannel.ChannelReserveSatoshis)
            {
                return new ErrorCloseChannelResponse(acceptChannel.TemporaryChannelId, "channel_reserve_satoshis is less than dust_limit_satoshis within the open_channel message");
            }

            if (channelCandidate.OpenChannel.DustLimitSatoshis > acceptChannel.ChannelReserveSatoshis)
            {
                return new ErrorCloseChannelResponse(acceptChannel.TemporaryChannelId, "channel_reserve_satoshis from the open_channel message is less than dust_limit_satoshis");
            }

            channelCandidate.AcceptChannel = acceptChannel;

            await _channelCandidateRepository.UpdateAsync(channelCandidate);

            var fundingScript = _lightningScripts.FundingWitnessScript(channelCandidate.OpenChannel.FundingPubkey, channelCandidate.AcceptChannel.FundingPubkey);

            channelCandidate.FundingTransaction =await _walletTransactions.GenerateTransactionForOutputAsync(new TransactionOutput
            {
                PublicKeyScript = fundingScript,
                Value = channelCandidate.OpenChannel.FundingSatoshis
            });
            
            var fundingTransactionHash = _transactionHashCalculator.ComputeHash(channelCandidate.FundingTransaction);
            var fundingTransactionOutputIndex = GetFundingTransactionOutputIndex(channelCandidate, fundingScript);

            _logger.LogDebug($"Channel point - {fundingTransactionHash} : {fundingTransactionOutputIndex} ");
            
            // David: this params can go in channel candidate
            var optionAnchorOutputs = _boltFeatures.SupportsFeature(Features.OptionAnchorOutputs);
            var optionStaticRemoteKey = _boltFeatures.SupportsFeature(Features.OptionStaticRemotekey); // not sure why this must be on if other side supports it and we don't

            Secret seed = _secretStore.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            var fundingOutPoint = new OutPoint { Hash = fundingTransactionHash, Index = fundingTransactionOutputIndex };

            _transactionBuilder.WithAcceptChannel(acceptChannel)
                .WithOpenChannel(channelCandidate.OpenChannel)
                .WithFundingOutpoint(fundingOutPoint)
                .WithFundingSide(channelCandidate.ChannelOpener);

            if (optionAnchorOutputs) _transactionBuilder.WithAnchorOutputs();
            if (optionStaticRemoteKey) _transactionBuilder.WithStaticRemoteKey();

            var remoteCommitmentTransaction = _transactionBuilder.BuildRemoteCommitmentTransaction();

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
                var trxhex = _serializationFactory.Serialize(channelCandidate.RemoteCommitmentTransaction);
                _logger.LogDebug("RemoteCommitmentTransaction = {trxhex}", Hex.ToString(trxhex));
            }

            _logger.LogDebug("Remote Commitment signature = {remoteFundingSign}", remoteFundingSign);

            UInt256 newChannelId = _lightningKeyDerivation.DeriveChannelId(fundingTransactionHash, (ushort)fundingTransactionOutputIndex);

            _logger.LogDebug("New channelid = {newChannelId}", newChannelId);

            var fundingCreated = new FundingCreated
            {
                FundingTxid = fundingTransactionHash,
                FundingOutputIndex = (ushort)fundingTransactionOutputIndex,
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

            return new SuccessWithOutputResponse(boltMessage);
        }

        private static uint GetFundingTransactionOutputIndex(ChannelCandidate channelCandidate, byte[] fundingScript)
        {
            for (uint i = 0; i < channelCandidate.FundingTransaction.Outputs.Length; i++)
            {
                var output = channelCandidate.FundingTransaction.Outputs[i];
                if (output.Value != channelCandidate.OpenChannel.FundingSatoshis ||
                    !output.PublicKeyScript.SequenceEqual(fundingScript)) continue;
                return i;
            }

            throw new ArgumentOutOfRangeException(
                "Failed to find the funding transaction output in the transaction returned from the wallet");
        }
    }
}