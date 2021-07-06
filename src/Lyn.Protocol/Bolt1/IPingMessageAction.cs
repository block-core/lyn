using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public interface IPingMessageAction
    {
        int ActionTimeIntervalSeconds();

        Task<MessageProcessingOutput> GeneratePingMessageAsync(PublicKey nodeId, CancellationToken token);
    }
}