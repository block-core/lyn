using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages
{
    public class Shutdown : MessagePayload
    {
        public override MessageType MessageType => MessageType.Shutdown;
        public ChannelId? ChannelId { get; set; }
        public ushort? Lentgh { get; set; }
        public byte[]? ScriptPubkey { get; set; }
    }
}