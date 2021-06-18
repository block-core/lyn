using System.Threading;
using System.Threading.Tasks;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt1
{
    public interface ISetupMessageService<T> where T : BoltMessage
    {
        ValueTask<MessageProcessingOutput> ProcessMessageAsync(T message, CancellationToken cancellation);

        ValueTask<T> CreateNewMessageAsync();
    }
}