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
    public class UpdateFailMalformedHtlcMessageService : IBoltMessageService<UpdateFailMalformedHtlc>
    {
        private readonly ILogger<UpdateFailMalformedHtlcMessageService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;

        public UpdateFailMalformedHtlcMessageService(ILogger<UpdateFailMalformedHtlcMessageService> logger,
            IPaymentChannelRepository paymentChannelRepository)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateFailMalformedHtlc> message)
        {
            var updateFailMalformedHtlc = message.MessagePayload;

            if (updateFailMalformedHtlc.ChannelId is null)
                return new ErrorCloseChannelResponse(new UInt256(new byte[32]),
                    "Invalid update_fail_malformed_htlc message");

            var channel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(updateFailMalformedHtlc.ChannelId);

            if (channel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}",
                    updateFailMalformedHtlc.ChannelId);

                return new ErrorCloseChannelResponse(updateFailMalformedHtlc.ChannelId, "Channel not found");
            }

            // Per BOLT 2 the failure code must have the BADONION bit set (0x8000), otherwise this
            // message is a protocol violation and the channel must be failed.
            if (updateFailMalformedHtlc.FailureCode is null ||
                ((updateFailMalformedHtlc.FailureCode & 0x8000) == 0))
                return new ErrorCloseChannelResponse(updateFailMalformedHtlc.ChannelId,
                    "failure_code must contain the BADONION flag");

            var pendingHtlc = channel.PendingRemoteHtlcs.FirstOrDefault(h => h.Id == updateFailMalformedHtlc.Id);

            if (pendingHtlc is null)
                return new ErrorCloseChannelResponse(updateFailMalformedHtlc.ChannelId,
                    "Unknown HTLC id");

            channel.PendingRemoteHtlcs.Remove(pendingHtlc);

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("Remote reported malformed htlc with id {HtlcId} on channel {ChannelId}",
                updateFailMalformedHtlc.Id, updateFailMalformedHtlc.ChannelId);

            return new EmptySuccessResponse();
        }
    }
}
