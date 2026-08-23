using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.ChannelClose
{
    public class ClosingSignedMessageService : IBoltMessageService<ClosingSigned>
    {
        private readonly ILogger<ClosingSignedMessageService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;

        public ClosingSignedMessageService(ILogger<ClosingSignedMessageService> logger,
            IPaymentChannelRepository paymentChannelRepository)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ClosingSigned> message)
        {
            var closingSigned = message.MessagePayload;

            if (closingSigned.ChannelId is null || closingSigned.FeeSatoshis is null || closingSigned.Signature is null)
                return new ErrorCloseChannelResponse(closingSigned.ChannelId ?? new UInt256(new byte[32]),
                    "Invalid closing_signed message");

            var channel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(closingSigned.ChannelId);

            if (channel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}", closingSigned.ChannelId);

                return new ErrorCloseChannelResponse(closingSigned.ChannelId, "Channel not found");
            }

            if (!channel.ShutdownReceived || !channel.ShutdownInitiatedLocally)
                return new ErrorCloseChannelResponse(closingSigned.ChannelId,
                    "closing_signed received before mutual shutdown");

            channel.RemoteClosingFeeSatoshis = closingSigned.FeeSatoshis;

            // If we have not proposed a fee yet, accept the remote proposal as our own to
            // converge the negotiation quickly.
            if (channel.LocalClosingFeeSatoshis == default)
                channel.LocalClosingFeeSatoshis = closingSigned.FeeSatoshis;

            bool feesAgreed = (ulong)channel.LocalClosingFeeSatoshis == (ulong)channel.RemoteClosingFeeSatoshis;

            channel.RemoteClosingSignature = closingSigned.Signature;

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            if (!feesAgreed)
            {
                // Propose the midpoint between both fee proposals to converge the negotiation.
                var midpointFee = new Satoshis(
                    ((ulong)channel.LocalClosingFeeSatoshis + (ulong)channel.RemoteClosingFeeSatoshis) / 2);
                channel.LocalClosingFeeSatoshis = midpointFee;

                await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

                var counterProposal = new BoltMessage
                {
                    Payload = new ClosingSigned
                    {
                        ChannelId = channel.ChannelId,
                        FeeSatoshis = midpointFee,
                        Signature = new CompressedSignature()
                    }
                };

                return new SuccessWithOutputResponse(counterProposal);
            }

            // Both peers agree on the fee, the cooperative closing transaction can now be
            // signed and published by the wallet.
            _logger.LogInformation("Cooperative close fee agreed for channel {ChannelId}", closingSigned.ChannelId);

            return new EmptySuccessResponse();
        }

        public async Task<MessageProcessingOutput> InitiateClosingFeeProposalAsync(PaymentChannel channel)
        {
            var closingSigned = new BoltMessage
            {
                Payload = new ClosingSigned
                {
                    ChannelId = channel.ChannelId,
                    // Estimated closing transaction size is 724 weight units, convert feerate per kw.
                    FeeSatoshis = new Satoshis((ulong)channel.FeeratePerKw * 724 / 4),
                    Signature = new CompressedSignature()
                }
            };

            return await Task.FromResult(new SuccessWithOutputResponse(closingSigned));
        }
    }
}
