using Lyn.Protocol.Bolt1.Messages;

namespace Lyn.Protocol.Common.Messages
{
    public class BoltMessage
    {
        public MessageType Type => Payload.MessageType;
        
        public MessagePayload Payload { get; set; }
        
        public TlVStream? Extension { get; set; }
    }
}