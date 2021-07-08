using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages
{
    public class ClosingSigned : MessagePayload
    {
        public override MessageType MessageType => MessageType.ClosingSigned;
        public ChannelId? ChannelId { get; set; }
        public Satoshis? FeeSatoshis { get; set; }
        public CompressedSignature? Signature { get; set; }
    }
}