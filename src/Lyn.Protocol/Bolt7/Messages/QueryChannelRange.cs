using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt7.Messages
{
    /// <summary>
    /// Query for channel range - asks for short_channel_ids in a range of blocks.
    /// </summary>
    public class QueryChannelRange : GossipMessage
    {
        public QueryChannelRange()
        {
            MessageType = MessageType.QueryChannelRange;
        }

        public QueryChannelRange(UInt256 chainHash, uint firstBlocknum, uint numberOfBlocks)
        {
            ChainHash = chainHash;
            FirstBlocknum = firstBlocknum;
            NumberOfBlocks = numberOfBlocks;
            MessageType = MessageType.QueryChannelRange;
        }

        public override MessageType MessageType { get; }

        /// <summary>
        /// The genesis hash of the chain being queried.
        /// </summary>
        public UInt256? ChainHash { get; set; }

        /// <summary>
        /// The first block height to query.
        /// </summary>
        public uint FirstBlocknum { get; set; }

        /// <summary>
        /// The number of blocks to query.
        /// </summary>
        public uint NumberOfBlocks { get; set; }
    }
}

