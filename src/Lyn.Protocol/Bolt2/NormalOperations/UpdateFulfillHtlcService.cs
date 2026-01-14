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
    /// Handles incoming update_fulfill_htlc messages when a peer provides
    /// the preimage to claim an HTLC.
    /// </summary>
    public class UpdateFulfillHtlcService : IBoltMessageService<UpdateFulfillHtlc>
    {
        private readonly ILogger<UpdateFulfillHtlcService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;

        public UpdateFulfillHtlcService(
            ILogger<UpdateFulfillHtlcService> logger,
            IPaymentChannelRepository paymentChannelRepository)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateFulfillHtlc> message)
        {
            var fulfillHtlc = message.MessagePayload;

            if (fulfillHtlc.ChannelId == null)
            {
                _logger.LogError("Received update_fulfill_htlc with null channel_id");
                return new ErrorCloseChannelResponse(UInt256.Zero, "Invalid channel_id");
            }

            var channel = await _paymentChannelRepository.TryGetPaymentChannelAsync(fulfillHtlc.ChannelId);

            if (channel == null)
            {
                _logger.LogError("Channel {ChannelId} not found for update_fulfill_htlc", fulfillHtlc.ChannelId);
                return new ErrorCloseChannelResponse(fulfillHtlc.ChannelId, "Unknown channel");
            }

            if (fulfillHtlc.Id == null)
            {
                _logger.LogError("Received update_fulfill_htlc with null id");
                return new ErrorCloseChannelResponse(fulfillHtlc.ChannelId, "Invalid HTLC id");
            }

            // Find the HTLC being fulfilled
            var htlc = channel.Htlcs.FirstOrDefault(h => h.Id == fulfillHtlc.Id && h.Side == ChannelSide.Local);

            if (htlc == null)
            {
                _logger.LogError("HTLC {HtlcId} not found in channel {ChannelId}", fulfillHtlc.Id, fulfillHtlc.ChannelId);
                return new ErrorCloseChannelResponse(fulfillHtlc.ChannelId, "Unknown HTLC id");
            }

            if (fulfillHtlc.PaymentPreimage == null)
            {
                _logger.LogError("Received update_fulfill_htlc with null preimage");
                return new ErrorCloseChannelResponse(fulfillHtlc.ChannelId, "Invalid preimage");
            }

            // Validate preimage against payment hash
            if (!ValidatePreimage(htlc.Rhash, fulfillHtlc.PaymentPreimage))
            {
                _logger.LogError("Invalid preimage for HTLC {HtlcId}", fulfillHtlc.Id);
                return new ErrorCloseChannelResponse(fulfillHtlc.ChannelId, "Invalid preimage");
            }

            // Store the preimage and update HTLC state
            htlc.Preimage = fulfillHtlc.PaymentPreimage;
            htlc.State = HtlcState.RcvdRemoveHtlc;

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("HTLC {HtlcId} fulfilled with preimage for channel {ChannelId}",
                fulfillHtlc.Id, fulfillHtlc.ChannelId);

            return new EmptySuccessResponse();
        }

        private bool ValidatePreimage(UInt256 paymentHash, Lyn.Types.Fundamental.Preimage preimage)
        {
            // Hash the preimage and compare with payment hash
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(preimage.GetBytes().ToArray());
            var computedHash = new UInt256(hash);
            
            return computedHash.Equals(paymentHash);
        }
    }
}

