using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class FundingSigned : MessagePayload
    {
        public override MessageType MessageType => MessageType.FundingSigned;
        public ChannelId? ChannelId { get; set; }
        public CompressedSignature? Signature { get; set; }
    }
}