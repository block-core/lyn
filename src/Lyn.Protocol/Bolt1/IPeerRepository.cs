using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public interface IPeerRepository
    {
        Task AddNewPeerAsync(Peer peer);
        Task AddErrorMessageToPeerAsync(PublicKey nodeId, PeerCommunicationIssue errorMessage);
        bool PeerExists(PublicKey nodeId);
        Peer? TryGetPeerAsync(PublicKey nodeId);
        Task AddOrUpdatePeerAsync(Peer peer);
    }
}