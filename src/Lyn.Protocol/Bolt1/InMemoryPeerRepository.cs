using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Types.Bolt.Messages;
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

        public Peer GetPeer(PublicKey nodeId)
        {
            throw new System.NotImplementedException();
        }
    }
}