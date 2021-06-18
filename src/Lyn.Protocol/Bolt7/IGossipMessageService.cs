using System.Threading;
using System.Threading.Tasks;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt7
{
    public interface IGossipMessageService<in T> where T : GossipMessage
    {
        MessageProcessingOutput ProcessMessage(T message);

        ValueTask<MessageProcessingOutput> ProcessMessageAsync(T message, CancellationToken cancellation);
    }
}