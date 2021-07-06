using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Connection
{
    public interface IBoltMessageSender<T> where T : MessagePayload
    {
        Task SendMessageAsync(PeerMessage<T> message);
    }

    public class BoltMessageSender<T> : IBoltMessageSender<T> where T : MessagePayload
    {
        public Task SendMessageAsync(PeerMessage<T> message)
        {
            throw new System.NotImplementedException();
        }
    }
}