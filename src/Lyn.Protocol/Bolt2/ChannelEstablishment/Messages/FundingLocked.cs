using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class FundingLocked : BoltMessage
    {
        private const string COMMAND = "36";
        public override string Command => COMMAND;
        public ChannelId? ChannelId { get; set; }
        public PublicKey? NextPerCommitmentPoint { get; set; }
    }
}