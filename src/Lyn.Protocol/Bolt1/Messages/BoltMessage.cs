namespace Lyn.Protocol.Bolt1.Messages
{
    public class BoltMessage
    {
        public MessageType Type => Payload.MessageType;
        
        public MessagePayload Payload { get; set; }
        
        public TlVStream? Extension { get; set; }
    }
}