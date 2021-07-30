using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt1.Messages
{
    public class ErrorMessage : MessagePayload
    {
        public override MessageType MessageType => MessageType.Error;

        public UInt256 ChannelId { get; set; } = new(new byte[32]);

        public ushort Len { get; set; }

        public byte[]? Data { get; set; }
    }
}