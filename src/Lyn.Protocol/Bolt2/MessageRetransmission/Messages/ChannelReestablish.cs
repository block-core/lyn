using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.MessageRetransmission.Messages
{
    public class ChannelReestablish : MessagePayload
    {
        public override MessageType MessageType => MessageType.ChannelReestablish;
        public ChannelId? ChannelId { get; set; }
        public ulong? NextCommitmentNumber { get; set; }
        public ulong? NextRevocationNumber { get; set; }
        public Secret? YourLastPerCommitmentSecret { get; set; }
        public PublicKey? MyCurrentPerCommitmentPoint { get; set; }
    }
}