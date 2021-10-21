using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public interface IPaymentChannelRepository
    {
        Task AddNewPaymentChannelAsync(PaymentChannel paymentChannel);
        
        Task<PaymentChannel?> TryGetPaymentChannelAsync(UInt256 channelId);
    }
}