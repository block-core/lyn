using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Connection
{
    public class PeerMessage<T> where T : BoltMessage
    {
        public PublicKey? PeerId { get; set; }

        public T? Message { get; set; }
    }
}