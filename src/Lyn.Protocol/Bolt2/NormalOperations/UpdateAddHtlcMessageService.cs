using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public class UpdateAddHtlcMessageService : IBoltMessageService<UpdateAddHtlc>
    {
        private readonly ILogger<UpdateAddHtlcMessageService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly IChainConfigProvider _chainConfigProvider;

        public UpdateAddHtlcMessageService(ILogger<UpdateAddHtlcMessageService> logger,
            IPaymentChannelRepository paymentChannelRepository,
            IChainConfigProvider chainConfigProvider)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
            _chainConfigProvider = chainConfigProvider;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateAddHtlc> message)
        {
            var updateAddHtlc = message.MessagePayload;

            if (updateAddHtlc.ChannelId is null || updateAddHtlc.Id is null
                || updateAddHtlc.AmountMsat is null || updateAddHtlc.PaymentHash is null
                || updateAddHtlc.OnionRoutingPacket is null)
                return new ErrorCloseChannelResponse(updateAddHtlc.ChannelId ?? new UInt256(new byte[32]),
                    "Invalid update_add_htlc message");

            var channel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(updateAddHtlc.ChannelId);

            if (channel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}", updateAddHtlc.ChannelId);

                return new ErrorCloseChannelResponse(updateAddHtlc.ChannelId, "Channel not found");
            }

            if (channel.ShutdownReceived || channel.ShutdownInitiatedLocally)
                return new ErrorCloseChannelResponse(updateAddHtlc.ChannelId,
                    "Can't add htlc when closing a channel");

            if (updateAddHtlc.AmountMsat == new MiliSatoshis(0))
                return new ErrorCloseChannelResponse(updateAddHtlc.ChannelId, "amount_msat must be non-zero");

            if (updateAddHtlc.OnionRoutingPacket.Length !=
                UpdateAddHtlcSerializer.ONION_ROUTING_PACKET_LENGTH)
                return new ErrorCloseChannelResponse(updateAddHtlc.ChannelId,
                    "onion_routing_packet must be 1366 bytes");

            if (channel.PendingRemoteHtlcs.Any(h => h.Id == updateAddHtlc.Id))
                return new ErrorCloseChannelResponse(updateAddHtlc.ChannelId,
                    "Duplicate HTLC id");

            if (updateAddHtlc.Id != channel.NextRemoteHtlcId)
                return new ErrorCloseChannelResponse(updateAddHtlc.ChannelId,
                    "HTLC id out of sequence");

            var chainParameters = _chainConfigProvider.GetConfiguration(new UInt256(new byte[32]));

            if (chainParameters != null && chainParameters.ChannelBoundariesConfig != null &&
                (ulong)updateAddHtlc.AmountMsat > chainParameters.ChannelConfig.MaxHtlcValueInFlight * 1000)
                return new ErrorCloseChannelResponse(updateAddHtlc.ChannelId,
                    "HTLC value exceeds maximum in flight");

            channel.NextRemoteHtlcId++;
            channel.PendingRemoteHtlcs.Add(updateAddHtlc);

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("Added remote htlc with id {HtlcId} to channel {ChannelId}",
                updateAddHtlc.Id, updateAddHtlc.ChannelId);

            return new EmptySuccessResponse();
        }
    }
}
