using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFailHtlc : MessagePayload
    {
        public override MessageType MessageType => MessageType.UpdateFailHtlc;
        public UInt256? ChannelId { get; set; }
        public ushort? Length { get; set; }
        public byte[]? Reason { get; set; }
    }
}