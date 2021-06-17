using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt1
{
    public class InMemoryPeerRepository : IPeerRepository
    {
        public ConcurrentDictionary<string, Peer> Peers = new ();
        
        public ConcurrentDictionary<string, List<ErrorMessage>> ErrorMessages = new ();
        
        public ValueTask AddNewPeerAsync(Peer peer)
        {
            Peers.TryAdd(peer.PeerId,peer);

            return new ValueTask();
        }

        public ValueTask AddErrorMessageToPeerAsync(string peerId, ErrorMessage errorMessage)
        {
            if (ErrorMessages.ContainsKey(peerId))
            {
                ErrorMessages[peerId].Add(errorMessage);
            }
            else
            {
                ErrorMessages.TryAdd(peerId, new List<ErrorMessage>{errorMessage});
            }

            return new ValueTask();
        }
    }
}