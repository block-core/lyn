using System.Collections.Concurrent;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public class InMemoryPaymentChannelRepository : IPaymentChannelRepository
    {
        private ConcurrentDictionary<UInt256, PaymentChannel> _channels;

        public InMemoryPaymentChannelRepository()
        {
            _channels = new ConcurrentDictionary<UInt256, PaymentChannel>();
        }

        public Task AddNewPaymentChannelAsync(PaymentChannel paymentChannel)
        {
            var success = _channels.TryAdd(paymentChannel.ChannelId, paymentChannel);

            if (!success)
            {
                //TODO David
            }
            
            return Task.CompletedTask;
        }

        public Task<PaymentChannel?> TryGetPaymentChannelAsync(UInt256 channelId)
        {
            return _channels.ContainsKey(channelId)
                ? Task.FromResult<PaymentChannel?>(_channels[channelId])
                : Task.FromResult<PaymentChannel?>(null);
            
        }
    }
}