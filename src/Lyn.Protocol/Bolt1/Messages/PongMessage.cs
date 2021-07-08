using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt1.Messages
{
    public class PongMessage : MessagePayload
    {
        public override MessageType MessageType => MessageType.Pong;

        public ushort BytesLen { get; set; }

        public byte[] Ignored { get; set; }

        public ushort Id => BytesLen;
    }
}