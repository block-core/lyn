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
    /// Handles incoming update_fail_malformed_htlc messages when a peer fails 
    /// an HTLC due to a malformed onion packet.
    /// </summary>
    public class UpdateFailMalformedHtlcService : IBoltMessageService<UpdateFailMalformedHtlc>
    {
        private readonly ILogger<UpdateFailMalformedHtlcService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;

        public UpdateFailMalformedHtlcService(
            ILogger<UpdateFailMalformedHtlcService> logger,
            IPaymentChannelRepository paymentChannelRepository)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateFailMalformedHtlc> message)
        {
            var failMalformedHtlc = message.MessagePayload;

            if (failMalformedHtlc.ChannelId == null)
            {
                _logger.LogError("Received update_fail_malformed_htlc with null channel_id");
                return new ErrorCloseChannelResponse(UInt256.Zero, "Invalid channel_id");
            }

            var channel = await _paymentChannelRepository.TryGetPaymentChannelAsync(failMalformedHtlc.ChannelId);

            if (channel == null)
            {
                _logger.LogError("Channel {ChannelId} not found for update_fail_malformed_htlc", failMalformedHtlc.ChannelId);
                return new ErrorCloseChannelResponse(failMalformedHtlc.ChannelId, "Unknown channel");
            }

            if (failMalformedHtlc.Id == null)
            {
                _logger.LogError("Received update_fail_malformed_htlc with null id");
                return new ErrorCloseChannelResponse(failMalformedHtlc.ChannelId, "Invalid HTLC id");
            }

            // BOLT 2: failure_code MUST have BADONION bit set
            if (failMalformedHtlc.FailureCode == null || (failMalformedHtlc.FailureCode & 0x8000) == 0)
            {
                _logger.LogError("Received update_fail_malformed_htlc with invalid failure_code (BADONION not set)");
                return new ErrorCloseChannelResponse(failMalformedHtlc.ChannelId, "Invalid failure_code");
            }

            // Find the HTLC being failed
            var htlc = channel.Htlcs.FirstOrDefault(h => h.Id == failMalformedHtlc.Id.Value && h.Side == ChannelSide.Local);

            if (htlc == null)
            {
                _logger.LogError("HTLC {HtlcId} not found in channel {ChannelId}", failMalformedHtlc.Id, failMalformedHtlc.ChannelId);
                return new ErrorCloseChannelResponse(failMalformedHtlc.ChannelId, "Unknown HTLC id");
            }

            // Store the failure code and SHA256 of onion
            htlc.FailureCode = failMalformedHtlc.FailureCode.Value;
            htlc.Sha256OfOnion = failMalformedHtlc.Sha256OfOnion;
            htlc.State = HtlcState.RcvdRemoveHtlc;

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("HTLC {HtlcId} failed (malformed) with failure_code {FailureCode} for channel {ChannelId}",
                failMalformedHtlc.Id, failMalformedHtlc.FailureCode, failMalformedHtlc.ChannelId);

            return new EmptySuccessResponse();
        }
    }
}

