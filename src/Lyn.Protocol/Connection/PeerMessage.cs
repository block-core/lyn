using System;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Connection
{
    public class PeerMessage<T> where T : MessagePayload
    {
        public PeerMessage(PublicKey nodeId, BoltMessage message)
        {
            NodeId = nodeId;
            Message = message;
            MessagePayload = message.Payload as T ?? throw new InvalidCastException($"{typeof(T).FullName} from {message.Payload.GetType()}");
        }

        public PublicKey NodeId { get; set; }

        private BoltMessage Message { get; set; }

        public T MessagePayload { get; set; }
    }
}