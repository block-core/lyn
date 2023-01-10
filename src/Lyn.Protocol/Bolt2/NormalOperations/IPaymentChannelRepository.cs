using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public interface IPaymentChannelRepository
    {
        Task AddNewPaymentChannelAsync(PaymentChannel paymentChannel);
        
        Task<PaymentChannel?> TryGetPaymentChannelAsync(UInt256 channelId);
        Task UpdatePaymentChannelAsync(PaymentChannel channel);
    }
}