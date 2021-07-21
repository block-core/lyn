using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt1.Messages
{
    public class ErrorMessage : MessagePayload
    {
        public override MessageType MessageType => MessageType.Error;

        public ChannelId ChannelId { get; set; } = new(new byte[32]);

        public ushort Len { get; set; }

        public byte[]? Data { get; set; }
    }
}