using System.Threading.Tasks;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Connection
{
    public interface IBoltValidationService<T> where T : BoltMessage
    {
        Task<bool> ValidateMessageAsync(PeerMessage<T> message);
    }
}