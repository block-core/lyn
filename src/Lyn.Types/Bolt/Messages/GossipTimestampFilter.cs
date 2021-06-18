using Lyn.Types.Fundamental;

namespace Lyn.Types.Bolt.Messages
{
    public class GossipTimestampFilter : GossipMessage
    {
        private const string COMMAND = "265";
        public override string Command => COMMAND;

        public ChainHash? ChainHash { get; set; }

        public uint FirstTimestamp { get; set; }

        public uint TimestampRange { get; set; }

        public PublicKey? NodeId { get; }
    }
}