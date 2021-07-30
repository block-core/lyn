using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFulfillHtlc : MessagePayload
    {
        public override MessageType MessageType => MessageType.UpdateFulfillHtlc;
        public UInt256? ChannelId { get; set; }
        public ulong? Id { get; set; }
        public Preimage? PaymentPreimage { get; set; }
    }
}