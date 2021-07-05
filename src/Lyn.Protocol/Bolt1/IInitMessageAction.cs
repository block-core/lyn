using System.Threading;
using System.Threading.Tasks;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public interface IInitMessageAction
    {
        Task SendInitAsync(PublicKey nodeId, CancellationToken token);
    }
}