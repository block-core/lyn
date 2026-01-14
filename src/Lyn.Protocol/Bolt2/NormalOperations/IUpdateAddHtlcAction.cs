using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public interface IUpdateAddHtlcAction
    {
        Task<MessageProcessingOutput> GenerateUpdateAddHtlcAsync(PublicKey nodeId, UInt256 channelId,
            MiliSatoshis amount,uint expiry, UInt256 paymentHash);
    }
}