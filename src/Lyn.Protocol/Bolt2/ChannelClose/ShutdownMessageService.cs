using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt2.ChannelClose
{
    public class ShutdownMessageService : IBoltMessageService<Shutdown>
    {
        private readonly IPaymentChannelRepository _channelRepository;
        private readonly IPeerRepository _peerRepository;

        public ShutdownMessageService(IPaymentChannelRepository channelRepository, IPeerRepository peerRepository)
        {
            _channelRepository = channelRepository;
            _peerRepository = peerRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<Shutdown> message)
        {
            if (message.MessagePayload.ChannelId != null)
            {
                await ClosePaymentChannelAsync(message.MessagePayload.ChannelId);
            }
            else
            {
                var peer = await _peerRepository.TryGetPeerAsync(message.NodeId);
                
                var closeChannelsTasks = peer.PaymentChannelIds.Select(ClosePaymentChannelAsync);

                await Task.WhenAll(closeChannelsTasks);
            }

            return new EmptySuccessResponse(); //TODO
        }

        private async Task ClosePaymentChannelAsync(UInt256 paymentChannelId)
        {
            var paymentChannel = await _channelRepository.TryGetPaymentChannelAsync(paymentChannelId);

            if (paymentChannel is null)
                throw new ArgumentNullException(nameof(paymentChannel));

            paymentChannel.CloseChannelTriggered = true;
            //TODO add the full logic (or service call) to shut down a channel
        }
    }
}