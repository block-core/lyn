using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt1.Messages
{
    public class WarningMessage: MessagePayload
    {
        public override MessageType MessageType => MessageType.Warning;

        public UInt256 ChannelId { get; set; } = new(new byte[32]);

        public ushort Len { get; set; }

        public byte[]? Data { get; set; }
    }
}