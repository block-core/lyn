using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public interface IInitMessageAction
    {
        Task<MessageProcessingOutput> GenerateInitAsync(PublicKey nodeId, CancellationToken token);
    }
}