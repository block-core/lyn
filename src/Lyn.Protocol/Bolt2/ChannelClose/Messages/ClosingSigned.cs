using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages
{
    public class ClosingSigned : MessagePayload
    {
        public override MessageType MessageType => MessageType.ClosingSigned;
        public UInt256? ChannelId { get; set; }
        public Satoshis? FeeSatoshis { get; set; }
        public CompressedSignature? Signature { get; set; }
    }
}