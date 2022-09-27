using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;

namespace Lyn.Protocol.Bolt2.ChannelClose
{
    public class CloseChannelMessageService : IBoltMessageService<ClosingSigned>
    {
        private readonly IPaymentChannelRepository _channelRepository;
        private readonly ILightningTransactions _lightningTransactions;

        public CloseChannelMessageService(IPaymentChannelRepository channelRepository,
            ILightningTransactions lightningTransactions)
        {
            _channelRepository = channelRepository;
            _lightningTransactions = lightningTransactions;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ClosingSigned> message)
        {
            var channel = await _channelRepository.TryGetPaymentChannelAsync(message.MessagePayload.ChannelId);

            if (channel is null)
                throw new ArgumentNullException(nameof(channel));

            if (!channel.CloseChannelTriggered)
                return new ErrorCloseChannelResponse(message.MessagePayload.ChannelId,
                    "Shutdown message was not received");

            if (!channel.Htlcs.Any())
            {
                
            }
            
            return new EmptySuccessResponse();
        }
    }
}