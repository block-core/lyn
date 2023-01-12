using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;

namespace Lyn.Protocol.Bolt2.ChannelClose
{
    public class ShutdownMessageService : IBoltMessageService<Shutdown>
    {
        private readonly IPaymentChannelRepository _channelRepository;
        private readonly IPeerRepository _peerRepository;
        private readonly IValidationHelper _validationHelper;

        public ShutdownMessageService(IPaymentChannelRepository channelRepository, IPeerRepository peerRepository, IValidationHelper validationHelper)
        {
            _channelRepository = channelRepository;
            _peerRepository = peerRepository;
            _validationHelper = validationHelper;
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
                  
            paymentChannel.ChannelShutdownTriggered = true;
            paymentChannel.CloseChannelDetails = new CloseChannelDetails
            {
                RemoteScriptPublicKey = message.MessagePayload.ScriptPubkey
            };

            if (paymentChannel.PendingHtlcs.Any())
                return new EmptySuccessResponse();
            
            return new SuccessWithOutputResponse(new BoltMessage
            {
                Payload = new Shutdown
                {
                    ChannelId = message.MessagePayload.ChannelId,
                    ScriptPubkey = paymentChannel.LocalFundingKey
                }
            });
        }
    }
}