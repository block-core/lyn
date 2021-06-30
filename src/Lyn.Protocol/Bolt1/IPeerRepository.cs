using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public interface IPeerRepository
    {
        Task AddNewPeerAsync(Peer peer);

        Task AddErrorMessageToPeerAsync(PublicKey nodeId, ErrorMessage errorMessage);

        Peer GetPeer(PublicKey nodeId);
    }
}