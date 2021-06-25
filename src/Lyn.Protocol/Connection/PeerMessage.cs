using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Connection
{
    public class PeerMessage<T> where T : BoltMessage
    {
        public PeerMessage(PublicKey nodeId, T message)
        {
            NodeId = nodeId;
            Message = message;
        }

        public PublicKey NodeId { get; set; }

        public T Message { get; set; }
    }
}