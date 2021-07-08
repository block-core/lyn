using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Connection
{
    public interface IBoltValidationService<T> where T : MessagePayload
    {
        Task<bool> ValidateMessageAsync(PeerMessage<T> message);
    }
}