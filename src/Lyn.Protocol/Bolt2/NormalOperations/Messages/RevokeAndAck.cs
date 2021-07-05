using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class RevokeAndAck : BoltMessage
    {
        private const string COMMAND = "133";
        public override string Command => COMMAND;
        public ChannelId? ChannelId { get; set; }
        public Secret? PerCommitmentSecret { get; set; }
        public PublicKey? NextPerCommitmentPoint { get; set; }
    }
}