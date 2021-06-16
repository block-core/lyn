using System.Threading;
using System.Threading.Tasks;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt2
{
    public interface INetworkMessageService<in T> where T : NetworkMessageBase
    {
        MessageProcessingOutput ProcessMessage(T message);
    }
}