using System.Threading;
using System.Threading.Tasks;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public interface IPingMessageAction
    {
        int ActionTimeIntervalSeconds();

        Task SendPingAsync(PublicKey nodeId, CancellationToken token);
    }
}