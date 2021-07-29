using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class GossipTimestampFilter : GossipMessage
    {
        public override MessageType MessageType => MessageType.GossipTimestampFilter;

        public UInt256? ChainHash { get; set; }

        public uint FirstTimestamp { get; set; }

        public uint TimestampRange { get; set; }
    }
}