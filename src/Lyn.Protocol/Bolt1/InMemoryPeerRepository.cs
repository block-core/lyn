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

        public ConcurrentDictionary<PublicKey, List<ErrorMessage>> ErrorMessages = new();

        public Task AddNewPeerAsync(Peer peer)
        {
            Peers.TryAdd(peer.NodeId, peer);

            return Task.CompletedTask;
        }

        public Task AddErrorMessageToPeerAsync(PublicKey peerId, ErrorMessage errorMessage)
        {
            if (ErrorMessages.ContainsKey(peerId))
            {
                ErrorMessages[peerId].Add(errorMessage);
            }
            else
            {
                ErrorMessages.TryAdd(peerId, new List<ErrorMessage> { errorMessage });
            }

            return Task.CompletedTask;
        }

        public bool PeerExists(PublicKey nodeId)
        {
            return Peers.ContainsKey(nodeId);
        }

        public Peer? TryGetPeerAsync(PublicKey nodeId)
        {
            var key = Peers.Keys.FirstOrDefault(_ => _.Equals(nodeId)); 
            
            return key != null ? Peers[key] : null; //Hack for quick debug
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