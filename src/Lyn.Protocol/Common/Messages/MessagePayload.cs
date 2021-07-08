using Lyn.Protocol.Bolt1.Messages;

namespace Lyn.Protocol.Common.Messages
{
    public abstract class MessagePayload
    {
        public abstract MessageType MessageType { get; }
    }
}