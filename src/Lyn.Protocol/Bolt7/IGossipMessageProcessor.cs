using System.Threading;
using System.Threading.Tasks;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt7
{
    public interface IGossipMessageProcessor<in T> where T : GossipBaseMessage
    {
        MessageProcessingOutput ProcessMessage(T message);
        
        ValueTask<MessageProcessingOutput> ProcessMessageAsync(T message, CancellationToken cancellation);
    }
}