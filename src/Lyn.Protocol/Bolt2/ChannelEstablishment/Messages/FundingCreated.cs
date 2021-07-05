using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class FundingCreated : MessagePayload
    {
        public override MessageType MessageType => MessageType.FundingCreated;
        public ChannelId? TemporaryChannelId { get; set; }
        public UInt256? FundingTxid { get; set; }
        public ushort? FundingOutputIndex { get; set; }
        public CompressedSignature? Signature { get; set; }
    }
}