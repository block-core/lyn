using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public class InMemoryPeerRepository : IPeerRepository
    {
        public ConcurrentDictionary<PublicKey, Peer> Peers = new();

        public ConcurrentDictionary<PublicKey, List<PeerCommunicationIssue>> ErrorMessages = new();

        public Task AddNewPeerAsync(Peer peer)
        {
            Peers.TryAdd(peer.NodeId, peer);

            return Task.CompletedTask;
        }

        public Task AddErrorMessageToPeerAsync(PublicKey peerId, PeerCommunicationIssue errorMessage)
        {
            if (ErrorMessages.ContainsKey(peerId))
            {
                ErrorMessages[peerId].Add(errorMessage);
            }
            else
            {
                ErrorMessages.TryAdd(peerId, new List<PeerCommunicationIssue> { errorMessage });
            }

            return Task.CompletedTask;
        }

        public bool PeerExists(PublicKey nodeId)
        {
            return Peers.ContainsKey(nodeId);
        }

        public Task<Peer?> TryGetPeerAsync(PublicKey nodeId)
        {
            var key = Peers.Keys.FirstOrDefault(_ => _.Equals(nodeId));

            return key != null 
                ? Task.FromResult<Peer?>(Peers[key])  
                : Task.FromResult<Peer?>(null);
        }

        public Task AddOrUpdatePeerAsync(Peer peer)
        {
            Peers.AddOrUpdate(peer.NodeId,
                key => peer,
                (key, existingPeer) =>
                {
                    peer.Id = existingPeer.Id;
                    return existingPeer;
                });

            return Task.CompletedTask;
        }
    }
}