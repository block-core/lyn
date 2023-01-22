using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.ChannelClose.Messages.TlvRecords;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Bolt2.Wallet;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelClose
{
    public class CloseChannelMessageService : IBoltMessageService<ClosingSigned>, ICloseSignedAction
    {
        private readonly IPaymentChannelRepository _channelRepository;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly IWalletTransactions _walletTransactions;
        private readonly ISecretStore _secretStore;
        private readonly ILightningKeyDerivation _keyDerivation;
        private readonly ILightningScripts _lightningScripts;
        private readonly IValidationHelper _validationHelper;

        public CloseChannelMessageService(IPaymentChannelRepository channelRepository,
            ILightningTransactions lightningTransactions, IWalletTransactions walletTransactions, ISecretStore secretStore, ILightningKeyDerivation keyDerivation, ILightningScripts lightningScripts, IValidationHelper validationHelper)
        {
            _channelRepository = channelRepository;
            _lightningTransactions = lightningTransactions;
            _walletTransactions = walletTransactions;
            _secretStore = secretStore;
            _keyDerivation = keyDerivation;
            _lightningScripts = lightningScripts;
            _validationHelper = validationHelper;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ClosingSigned> message)
        {
            var channel = await _channelRepository.TryGetPaymentChannelAsync(message.MessagePayload.ChannelId);

            if (channel is null)
                throw new ArgumentNullException(nameof(channel));

            if (!channel.ChannelShutdownTriggered)
                return new ErrorCloseChannelResponse(message.MessagePayload.ChannelId,
                    "Shutdown message was not received");

            if (!ValidateSignature(channel, message.MessagePayload.Signature))
                return new WarningResponse(message.MessagePayload.ChannelId, nameof(message.MessagePayload.Signature));
            
            if (channel.ChannelClosingSignSent)
            {
                channel.CloseChannelDetails.RemoteNodeScriptPubKeySignature = message.MessagePayload.Signature;

                if (message.MessagePayload.FeeSatoshis == null)
                    throw new ArgumentNullException();//TODO move this to the message validation part

                if (message.MessagePayload.FeeSatoshis == channel.CloseChannelDetails.FeeSatoshis)
                {
                    await CloseChannelBroadcastAsync(channel);

                    return new EmptySuccessResponse();
                }
                
                if (channel.WasChannelInitiatedLocally)
                {
                    if (message.MessagePayload.FeeSatoshis <= channel.CloseChannelDetails.MaxFeeRange &&
                        message.MessagePayload.FeeSatoshis >= channel.CloseChannelDetails.MinFeeRange)
                    {
                        channel.CloseChannelDetails.FeeSatoshis = message.MessagePayload.FeeSatoshis;
                        
                        await CloseChannelBroadcastAsync(channel);
                    }
                    else
                    {
                        throw new InvalidOperationException("fee must be in the range"); //TODO must fail the channel
                    }
                }
                else
                {
                    if (message.MessagePayload.FeeSatoshis != channel.CloseChannelDetails.FeeSatoshis)
                    {
                        throw new InvalidOperationException("fee must be the same as sent in previous message"); //TODO must fail the channel 
                    }
                }
            }
            else
            {
                var range = GetFeeRange(message);

                channel.CloseChannelDetails = new CloseChannelDetails
                {
                    FeeSatoshis = range.MinFeeRange,
                    RemoteNodeScriptPubKeySignature = message.MessagePayload.Signature
                };

                await _channelRepository.UpdatePaymentChannelAsync(channel);
                
                var response = CloseChannelSignedResponse(channel);

                return new SuccessWithOutputResponse(new BoltMessage
                {
                    Payload = response
                });
            }

            return new EmptySuccessResponse();
        }

        private bool ValidateSignature(PaymentChannel channel, CompressedSignature signature)
        {
            var transactio = GetClosingTransaction(channel);
            
            return  _validationHelper.VerifySignature(channel.CloseChannelDetails.RemoteScriptPublicKey,
                signature,transactio.Hash);
        }
        
        private static FeeRange GetFeeRange(PeerMessage<ClosingSigned> message)
        {
            if (message.Message.Extension == null)
                throw new ArgumentNullException(nameof(message.Message.Extension)); //TODO should we just force close the channel?
            
            return message.Message.Extension.Records.Single(_ => _.Type == 1) as FeeRange; //TODO add type for the record
        }

        private ClosingSigned CloseChannelSignedResponse(PaymentChannel channel)
        {
            var transaction = GetClosingTransaction(channel);
            
            var seed = _secretStore.GetSeed();
            var secrets = _keyDerivation.DeriveSecrets(seed);

            var redeemScript = _lightningScripts.FundingRedeemScript(channel.LocalFundingKey, channel.RemoteFundingKey);
            var signedInput = _lightningTransactions.SignInput(transaction, secrets.FundingPrivkey, channel.InPoint.Index, redeemScript,
                channel.FundingSatoshis);

            return  new ClosingSigned
                {
                    ChannelId = channel.ChannelId,
                    FeeSatoshis = channel.CloseChannelDetails.FeeSatoshis,
                    Signature = _lightningTransactions.ToCompressedSignature(signedInput)
                };
        }
        
        private async Task CloseChannelBroadcastAsync(PaymentChannel channel)
        {
            var transaction = GetClosingTransaction(channel);

            await _walletTransactions.PublishTransactionAsync(transaction);
        }

        private Transaction GetClosingTransaction(PaymentChannel channel)
        {
            var localReceived = channel.Htlcs
                .Where(_ => _.Side == ChannelSide.Local)
                .Select(_ => (uint)_.AmountMsat)
                .Sum(_ => _);

            var remoteReceived = channel.Htlcs
                .Where(_ => _.Side == ChannelSide.Remote)
                .Select(_ => (uint)_.AmountMsat)
                .Sum(_ => _);

            var localAmount = channel.WasChannelInitiatedLocally //TODO fix the value here
                ? channel.FundingSatoshis - localReceived
                : localReceived;
                
            var remoteAmount = channel.WasChannelInitiatedLocally
                ? channel.FundingSatoshis - remoteReceived
                : remoteReceived;

            var transaction = _lightningTransactions.ClosingTransaction(new ClosingTransactionIn
            {
                Fee = channel.CloseChannelDetails.FeeSatoshis,
                FundingCreatedTxout = channel.InPoint,
                LocalSpendingSignature = new BitcoinSignature(new byte[]{}),//TODO
                RemoteSpendingSignature = _lightningTransactions.FromCompressedSignature(channel.CloseChannelDetails.RemoteNodeScriptPubKeySignature),
                AmountToPayLocal = localAmount,
                AmountToPayRemote = remoteAmount, 
                LocalScriptPublicKey = channel.CloseChannelDetails.LocalScriptPublicKey, //TODO need to verify that this is correct
                RemoteScriptPublicKey = channel.CloseChannelDetails.RemoteScriptPublicKey,
                SideThatOpenedChannel = channel.WasChannelInitiatedLocally ? ChannelSide.Local : ChannelSide.Remote
            });
            return transaction;
        }

        public async Task<MessageProcessingOutput> GenerateClosingSignedAsync(PublicKey nodeId, UInt256 channelId, CancellationToken token)
        {
            var paymentChannel = await _channelRepository.TryGetPaymentChannelAsync(channelId); //TODO add the node id as part of the payment channel

            if (!paymentChannel.ChannelShutdownTriggered)
                throw new InvalidOperationException("Payment channel not in the shut down state");
            
            if (paymentChannel.ChannelClosingSignSent)
                throw new InvalidOperationException("closing signed already sent");
            
            paymentChannel.CloseChannelDetails.FeeSatoshis = await _walletTransactions.GetMinimumFeeAsync();
            
            var response = CloseChannelSignedResponse(paymentChannel);

            return new MessageProcessingOutput
            {
                Success = true,
                CloseChannel = false,
                ResponseMessages = new[]
                {
                    new BoltMessage
                    {
                        Payload = response,
                        Extension = new TlVStream
                        {
                            Records = new List<TlvRecord>
                            {
                                new FeeRange
                                {
                                    MaxFeeRange = 2000, //TODO set actual values from the wallet
                                    MinFeeRange = 900
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}