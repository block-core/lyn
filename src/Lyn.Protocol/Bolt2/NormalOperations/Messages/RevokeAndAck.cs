using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class RevokeAndAck : MessagePayload
    {
        public override MessageType MessageType => MessageType.RevokeAndAck;
        public UInt256? ChannelId { get; set; }
        public Secret? PerCommitmentSecret { get; set; }
        public PublicKey? NextPerCommitmentPoint { get; set; }
    }
}