using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFailMalformedHtlc : MessagePayload
    {
        public override MessageType MessageType => MessageType.UpdateFailMalformedHtlc;
        public ChannelId? ChannelId { get; set; }
        public ulong? Id { get; set; }
        public UInt256? Sha256OfOnion { get; set; }
        public ushort? FailureCode { get; set; }
    }
}