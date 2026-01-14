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
    /// Handles incoming update_fee messages for dynamic fee updates.
    /// Only the channel funder can send update_fee messages.
    /// </summary>
    public class UpdateFeeService : IBoltMessageService<UpdateFee>
    {
        private readonly ILogger<UpdateFeeService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;

        public UpdateFeeService(
            ILogger<UpdateFeeService> logger,
            IPaymentChannelRepository paymentChannelRepository)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateFee> message)
        {
            var updateFee = message.MessagePayload;

            if (updateFee.ChannelId == null)
            {
                _logger.LogError("Received update_fee with null channel_id");
                return new ErrorCloseChannelResponse(UInt256.Zero, "Invalid channel_id");
            }

            var channel = await _paymentChannelRepository.TryGetPaymentChannelAsync(updateFee.ChannelId);

            if (channel == null)
            {
                _logger.LogError("Channel {ChannelId} not found for update_fee", updateFee.ChannelId);
                return new ErrorCloseChannelResponse(updateFee.ChannelId, "Unknown channel");
            }

            // BOLT 2: Only the funder can send update_fee
            // If we initiated the channel (we are funder), remote should not send update_fee
            if (channel.WasChannelInitiatedLocally)
            {
                _logger.LogError("Received update_fee from non-funder for channel {ChannelId}", updateFee.ChannelId);
                return new ErrorCloseChannelResponse(updateFee.ChannelId, "update_fee from non-funder");
            }

            if (updateFee.FeeratePerKw == null)
            {
                _logger.LogError("Received update_fee with null feerate_per_kw");
                return new ErrorCloseChannelResponse(updateFee.ChannelId, "Invalid feerate_per_kw");
            }

            // Validate fee rate is reasonable
            if (!ValidateFeeRate(updateFee.FeeratePerKw.Value))
            {
                _logger.LogError("Received update_fee with unreasonable feerate_per_kw: {FeeratePerKw}",
                    updateFee.FeeratePerKw);
                return new ErrorCloseChannelResponse(updateFee.ChannelId, "Unreasonable feerate_per_kw");
            }

            var oldFeeRate = channel.FeeratePerKw;
            channel.FeeratePerKw = updateFee.FeeratePerKw.Value;

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("Fee rate updated from {OldFeeRate} to {NewFeeRate} for channel {ChannelId}",
                oldFeeRate, updateFee.FeeratePerKw, updateFee.ChannelId);

            return new EmptySuccessResponse();
        }

        private bool ValidateFeeRate(uint feeratePerKw)
        {
            // BOLT 2: The receiving node MUST fail the channel if:
            // - feerate_per_kw is unreasonably small
            // - feerate_per_kw is unreasonably large
            
            const uint MinFeeratePerKw = 253; // Minimum practical fee rate
            const uint MaxFeeratePerKw = 100000000; // Very high but not overflow

            if (feeratePerKw < MinFeeratePerKw)
            {
                _logger.LogWarning("Fee rate {FeeratePerKw} is below minimum {MinFeeratePerKw}",
                    feeratePerKw, MinFeeratePerKw);
                return false;
            }

            if (feeratePerKw > MaxFeeratePerKw)
            {
                _logger.LogWarning("Fee rate {FeeratePerKw} is above maximum {MaxFeeratePerKw}",
                    feeratePerKw, MaxFeeratePerKw);
                return false;
            }

            return true;
        }
    }
}

