namespace Lyn.Protocol.Bolt1.Messages
{
    public abstract class MessagePayload
    {
        public abstract MessageType MessageType { get; }
    }
}