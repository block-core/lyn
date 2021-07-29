using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages
{
    public class Shutdown : MessagePayload
    {
        public override MessageType MessageType => MessageType.Shutdown;
        public UInt256? ChannelId { get; set; }
        public ushort? Lentgh { get; set; }
        public byte[]? ScriptPubkey { get; set; }
    }
}