using System.Collections.Generic;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public interface IPaymentChannelRepository
    {
        Task AddNewPaymentChannelAsync(PaymentChannel paymentChannel);
        
        Task<PaymentChannel?> TryGetPaymentChannelAsync(UInt256 channelId);
        Task UpdatePaymentChannelAsync(PaymentChannel channel);
        
        /// <summary>
        /// Gets all payment channels associated with a specific peer.
        /// </summary>
        /// <param name="nodeId">The node ID of the peer.</param>
        /// <returns>A collection of payment channels for the peer.</returns>
        Task<IEnumerable<PaymentChannel>> GetAllChannelsForPeerAsync(PublicKey nodeId);
    }
}