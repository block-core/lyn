using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt1
{
    public interface IPeerRepository
    {
        ValueTask AddNewPeerAsync(Peer peer);

        ValueTask AddErrorMessageToPeerAsync(string peerId, ErrorMessage errorMessage);
    }
}