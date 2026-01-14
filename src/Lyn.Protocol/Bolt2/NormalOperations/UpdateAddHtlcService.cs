using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public class UpdateAddHtlcService : IBoltMessageService<UpdateAddHtlc>, IUpdateAddHtlcAction
    {
        private IPaymentChannelRepository _paymentChannelRepository;
        private readonly ILogger<UpdateAddHtlcService> _logger;

        public UpdateAddHtlcService(IPaymentChannelRepository paymentChannelRepository, ILogger<UpdateAddHtlcService> logger)
        {
            _paymentChannelRepository = paymentChannelRepository;
            _logger = logger;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<UpdateAddHtlc> message)
        {
            var paymentChannel = await _paymentChannelRepository.TryGetPaymentChannelAsync(message.MessagePayload.ChannelId);

            if (paymentChannel == null)
            {
                _logger.LogInformation($"UpdateAddHtlc for unkowen payment channel with id - {message.MessagePayload.ChannelId}");
                return new ErrorCloseChannelResponse(message.MessagePayload.ChannelId, "Channel not found");
            }
            
            paymentChannel.Htlcs.Add(new Htlc
            {
                Id = message.MessagePayload.Id.Value,
                State = HtlcState.RcvdAddHtlc,
                AmountMsat = message.MessagePayload.AmountMsat,
                Expirylocktime = message.MessagePayload.CltvExpiry.Value,
                Rhash = message.MessagePayload.PaymentHash,
            });


            await _paymentChannelRepository.UpdatePaymentChannelAsync(paymentChannel);

            return new EmptySuccessResponse();
        }

        public async Task<MessageProcessingOutput> GenerateUpdateAddHtlcAsync(PublicKey nodeId, UInt256 channelId,
            MiliSatoshis amount,uint expiry, UInt256 paymentHash)
        {
            var paymentChannel = await _paymentChannelRepository.TryGetPaymentChannelAsync(channelId);
            
            if (paymentChannel == null)
            {
                _logger.LogInformation($"UpdateAddHtlc for unkowen payment channel with id - {channelId}");
                return new ErrorCloseChannelResponse(channelId, "Channel not found");
            }
            
            paymentChannel.Htlcs.Add(new Htlc
            {
                Id = (ulong)paymentChannel.Htlcs.Count(_ => _.Side == ChannelSide.Local),
                State = HtlcState.RcvdAddHtlc,
                AmountMsat = amount,
                Expirylocktime = expiry,
                Rhash = paymentHash,
            });


            await _paymentChannelRepository.UpdatePaymentChannelAsync(paymentChannel);

            return new EmptySuccessResponse();
        }
    }
}