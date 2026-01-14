using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    /// <summary>
    /// Handles incoming update_fail_htlc messages when a peer fails an HTLC.
    /// </summary>
    public class UpdateFailHtlcService : IBoltMessageService<UpdateFailHtlc>
    {
        private readonly ILogger<UpdateFailHtlcService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;

        public UpdateFailHtlcService(
            ILogger<UpdateFailHtlcService> logger,
            IPaymentChannelRepository paymentChannelRepository)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateFailHtlc> message)
        {
            var failHtlc = message.MessagePayload;

            if (failHtlc.ChannelId == null)
            {
                _logger.LogError("Received update_fail_htlc with null channel_id");
                return new ErrorCloseChannelResponse(UInt256.Zero, "Invalid channel_id");
            }

            var channel = await _paymentChannelRepository.TryGetPaymentChannelAsync(failHtlc.ChannelId);

            if (channel == null)
            {
                _logger.LogError("Channel {ChannelId} not found for update_fail_htlc", failHtlc.ChannelId);
                return new ErrorCloseChannelResponse(failHtlc.ChannelId, "Unknown channel");
            }

            if (failHtlc.Id == null)
            {
                _logger.LogError("Received update_fail_htlc with null id");
                return new ErrorCloseChannelResponse(failHtlc.ChannelId, "Invalid HTLC id");
            }

            // Find the HTLC being failed
            var htlc = channel.Htlcs.FirstOrDefault(h => h.Id == failHtlc.Id.Value && h.Side == ChannelSide.Local);

            if (htlc == null)
            {
                _logger.LogError("HTLC {HtlcId} not found in channel {ChannelId}", failHtlc.Id, failHtlc.ChannelId);
                return new ErrorCloseChannelResponse(failHtlc.ChannelId, "Unknown HTLC id");
            }

            // Store the failure reason and update HTLC state
            htlc.FailureReason = failHtlc.Reason;
            htlc.State = HtlcState.RcvdRemoveHtlc;

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("HTLC {HtlcId} failed for channel {ChannelId}", failHtlc.Id, failHtlc.ChannelId);

            return new EmptySuccessResponse();
        }
    }
}

