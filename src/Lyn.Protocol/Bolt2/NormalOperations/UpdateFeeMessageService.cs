using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public class UpdateFeeMessageService : IBoltMessageService<UpdateFee>
    {
        private readonly ILogger<UpdateFeeMessageService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly IChainConfigProvider _chainConfigProvider;

        public UpdateFeeMessageService(ILogger<UpdateFeeMessageService> logger,
            IPaymentChannelRepository paymentChannelRepository,
            IChainConfigProvider chainConfigProvider)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
            _chainConfigProvider = chainConfigProvider;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateFee> message)
        {
            var updateFee = message.MessagePayload;

            if (updateFee.ChannelId is null)
                return new ErrorCloseChannelResponse(new UInt256(new byte[32]), "Invalid update_fee message");

            var channel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(updateFee.ChannelId);

            if (channel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}", updateFee.ChannelId);

                return new ErrorCloseChannelResponse(updateFee.ChannelId, "Channel not found");
            }

            if (channel.ShutdownReceived || channel.ShutdownInitiatedLocally)
                return new ErrorCloseChannelResponse(updateFee.ChannelId,
                    "Can't update fee when closing a channel");

            // Only the channel opener is allowed to send update_fee (BOLT 2). If we initiated
            // this channel locally then a remote update_fee is a protocol violation.
            if (channel.WasChannelInitiatedLocally)
                return new ErrorCloseChannelResponse(updateFee.ChannelId,
                    "Only the channel opener may send update_fee");

            var chainParameters =
                _chainConfigProvider.GetConfiguration(new UInt256(new byte[32]));

            if (chainParameters?.ChannelBoundariesConfig != null)
            {
                var boundaries = chainParameters.ChannelBoundariesConfig;

                if (updateFee.FeeratePerKw < (uint)boundaries.TooLowFeeratePerKw ||
                    updateFee.FeeratePerKw > (uint)boundaries.TooLargeFeeratePerKw * 10)
                    return new ErrorCloseChannelResponse(updateFee.ChannelId, "Feerate out of acceptable range");
            }

            channel.FeeratePerKw = new Satoshis(updateFee.FeeratePerKw ?? 0);

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("Updated feerate to {FeeratePerKw} for channel {ChannelId}",
                updateFee.FeeratePerKw, updateFee.ChannelId);

            return new EmptySuccessResponse();
        }
    }
}
