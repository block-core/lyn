using System;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Connection
{
    public class PeerMessage<T> where T : MessagePayload
    {
        public PeerMessage(PublicKey nodeId, BoltMessage message)
        {
            NodeId = nodeId;
            Message = message;

            MessagePayload = message.Payload as T ??
                           throw new InvalidCastException($"{message.Payload.GetType()} as {typeof(T).FullName}");
        }

        public PublicKey NodeId { get; }

        public BoltMessage Message { get; }

        public T MessagePayload  { get; }
    }
}