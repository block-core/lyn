using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public class UpdateFailHtlcMessageService : IBoltMessageService<UpdateFailHtlc>
    {
        private readonly ILogger<UpdateFailHtlcMessageService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;

        public UpdateFailHtlcMessageService(ILogger<UpdateFailHtlcMessageService> logger,
            IPaymentChannelRepository paymentChannelRepository)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateFailHtlc> message)
        {
            var updateFailHtlc = message.MessagePayload;

            if (updateFailHtlc.ChannelId is null)
                return new ErrorCloseChannelResponse(new UInt256(new byte[32]), "Invalid update_fail_htlc message");

            var channel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(updateFailHtlc.ChannelId);

            if (channel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}", updateFailHtlc.ChannelId);

                return new ErrorCloseChannelResponse(updateFailHtlc.ChannelId, "Channel not found");
            }

            if (channel.ShutdownReceived || channel.ShutdownInitiatedLocally)
                return new ErrorCloseChannelResponse(updateFailHtlc.ChannelId,
                    "Can't fail htlc when closing a channel");

            var pendingHtlc = channel.PendingRemoteHtlcs.FirstOrDefault(h => h.Id == updateFailHtlc.Id);

            if (pendingHtlc is null)
                return new ErrorCloseChannelResponse(updateFailHtlc.ChannelId,
                    "Unknown HTLC id");

            channel.PendingRemoteHtlcs.Remove(pendingHtlc);

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("Remote failed htlc with id {HtlcId} on channel {ChannelId}",
                updateFailHtlc.Id, updateFailHtlc.ChannelId);

            return new EmptySuccessResponse();
        }
    }
}
