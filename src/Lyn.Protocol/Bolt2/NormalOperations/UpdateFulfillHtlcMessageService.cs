using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Common.Hashing;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public class UpdateFulfillHtlcMessageService : IBoltMessageService<UpdateFulfillHtlc>
    {
        private readonly ILogger<UpdateFulfillHtlcMessageService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;

        public UpdateFulfillHtlcMessageService(ILogger<UpdateFulfillHtlcMessageService> logger,
            IPaymentChannelRepository paymentChannelRepository)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateFulfillHtlc> message)
        {
            var updateFulfillHtlc = message.MessagePayload;

            if (updateFulfillHtlc.ChannelId is null || updateFulfillHtlc.Id is null || updateFulfillHtlc.PaymentPreimage is null)
                return new ErrorCloseChannelResponse(updateFulfillHtlc.ChannelId ?? new UInt256(new byte[32]),
                    "Invalid update_fulfill_htlc message");

            var channel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(updateFulfillHtlc.ChannelId);

            if (channel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}", updateFulfillHtlc.ChannelId);

                return new ErrorCloseChannelResponse(updateFulfillHtlc.ChannelId, "Channel not found");
            }

            if (channel.ShutdownReceived || channel.ShutdownInitiatedLocally)
                return new ErrorCloseChannelResponse(updateFulfillHtlc.ChannelId,
                    "Can't fulfill htlc when closing a channel");

            var pendingHtlc = channel.PendingRemoteHtlcs.FirstOrDefault(h => h.Id == updateFulfillHtlc.Id);

            if (pendingHtlc is null)
                return new ErrorCloseChannelResponse(updateFulfillHtlc.ChannelId,
                    "Unknown HTLC id");

            var preimageHash = HashGenerator.Sha256AsUInt256((byte[])updateFulfillHtlc.PaymentPreimage);

            if (!preimageHash.Equals(pendingHtlc.PaymentHash))
                return new ErrorCloseChannelResponse(updateFulfillHtlc.ChannelId,
                    "Preimage does not match payment hash");

            channel.PendingRemoteHtlcs.Remove(pendingHtlc);
            channel.FulfilledHtlcs.Add(updateFulfillHtlc);

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("Remote fulfilled htlc with id {HtlcId} on channel {ChannelId}",
                updateFulfillHtlc.Id, updateFulfillHtlc.ChannelId);

            return new EmptySuccessResponse();
        }
    }
}
