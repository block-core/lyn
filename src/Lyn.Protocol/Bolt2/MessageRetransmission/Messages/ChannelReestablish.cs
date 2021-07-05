using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.MessageRetransmission.Messages
{
    public class ChannelReestablish : BoltMessage
    {
        private const string COMMAND = "136";
        public override string Command => COMMAND;
        public ChannelId? ChannelId { get; set; }
        public ulong? NextCommitmentNumber { get; set; }
        public ulong? NextRevocationNumber { get; set; }
        public Secret? YourLastPerCommitmentSecret { get; set; }
        public PublicKey? MyCurrentPerCommitmentPoint { get; set; }
    }
}