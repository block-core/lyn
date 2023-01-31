using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using NBitcoin;

namespace Lyn.Protocol.Bolt2.ChannelClose
{
    public class ShutdownMessageService : IBoltMessageService<Shutdown>, IShutdownAction
    {
        private readonly IPaymentChannelRepository _channelRepository;
        private readonly IPeerRepository _peerRepository;
        private readonly IValidationHelper _validationHelper;
        private readonly ISecretStore _secretStore;
        private readonly ILightningKeyDerivation _keyDerivation;

        public ShutdownMessageService(IPaymentChannelRepository channelRepository, IPeerRepository peerRepository, IValidationHelper validationHelper, ISecretStore secretStore, ILightningKeyDerivation keyDerivation)
        {
            _channelRepository = channelRepository;
            _peerRepository = peerRepository;
            _validationHelper = validationHelper;
            _secretStore = secretStore;
            _keyDerivation = keyDerivation;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<Shutdown> message)
        {
            if (message.MessagePayload.ChannelId == null)
                throw new ArgumentNullException(nameof(message.MessagePayload.ChannelId));

            var paymentChannel = await _channelRepository.TryGetPaymentChannelAsync(message.MessagePayload.ChannelId);

            if (paymentChannel is null)
                throw new ArgumentNullException(nameof(paymentChannel)); //TODO David Do we need an exception here?

            if (!_validationHelper.ValidateScriptPubKeyP2WSHOrP2WPKH(message.MessagePayload.ScriptPubkey))
                return new WarningResponse(message.MessagePayload.ChannelId,"ScriptPubKey failed validation");
                  
            paymentChannel.ChannelShutdownTriggered = true; //TODO check what should be done if we sent one first
            paymentChannel.CloseChannelDetails = new CloseChannelDetails
            {
                RemoteScriptPublicKey = message.MessagePayload.ScriptPubkey
            };

            if (paymentChannel.PendingHtlcs?.Any() ?? false)
                return new EmptySuccessResponse();
            
            var script = GetScriptPubKey(paymentChannel);

            paymentChannel.CloseChannelDetails.LocalScriptPublicKey = script;

            await _channelRepository.UpdatePaymentChannelAsync(paymentChannel);
            
            return new SuccessWithOutputResponse(new BoltMessage
            {
                Payload = new Shutdown
                {
                    ChannelId = message.MessagePayload.ChannelId,
                    Length = (ushort)script.Length,
                    ScriptPubkey = script
                }
            });
        }

        private byte[] GetScriptPubKey(PaymentChannel paymentChannel)
        {
            var seed = _secretStore.GetSeed();

            var redeemPrivateKey = _keyDerivation.PerCommitmentSecret(new UInt256(seed), paymentChannel.LocalCommitmentNumber);
            var redeemPubKey = _keyDerivation.PublicKeyFromPrivateKey(redeemPrivateKey);

            var pubKey = new PubKey(redeemPubKey.ToString());
            
            return PayToWitPubKeyHashTemplate.Instance.GenerateScriptPubKey(pubKey)
                .ToBytes();
        }

        public async Task<MessageProcessingOutput> GenerateShutdownAsync(PublicKey nodeId, UInt256 channelId, CancellationToken token)
        {
            var paymentChannel = await _channelRepository.TryGetPaymentChannelAsync(channelId);
            
            if (paymentChannel is null)
                throw new ArgumentNullException(nameof(paymentChannel)); //TODO David Do we need an exception here?

            if (paymentChannel.ChannelShutdownTriggered)
                return new EmptySuccessResponse();

            paymentChannel.ChannelShutdownTriggered = true;
            
            var script = GetScriptPubKey(paymentChannel);

            paymentChannel.CloseChannelDetails ??= new CloseChannelDetails();
            
            paymentChannel.CloseChannelDetails.LocalScriptPublicKey = script;

            await _channelRepository.UpdatePaymentChannelAsync(paymentChannel);
            
            return new SuccessWithOutputResponse
            {
                ResponseMessages = new[]
                {
                    new BoltMessage
                    {
                        Payload = new Shutdown
                        {
                            ChannelId = channelId,
                            Length = (ushort)script.Length,
                            ScriptPubkey = script
                        }
                    }
                }
            };
        }
    }
}