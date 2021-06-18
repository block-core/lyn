using System.Threading.Tasks;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Connection
{
    public interface IBoltMessageSender<T> where T : BoltMessage
    {
        Task SendMessageAsync(PeerMessage<T> message);
    }
}