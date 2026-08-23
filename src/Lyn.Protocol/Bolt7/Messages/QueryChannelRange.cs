using System;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class QueryChannelRange : GossipMessage
    {
        public QueryChannelRange()
        {
            ChainHash = ChainHashes.Bitcoin;
        }

        public QueryChannelRange(UInt256 chainHash, uint firstBlocknum, uint numberOfBlocks)
        {
            ChainHash = chainHash;
            FirstBlocknum = firstBlocknum;
            NumberOfBlocks = numberOfBlocks;
        }

        public override MessageType MessageType => MessageType.QueryChannelRange;

        public UInt256 ChainHash { get; set; }

        /// <summary>
        /// The first block in the range of blocks to query.
        /// </summary>
        public uint FirstBlocknum { get; set; }

        /// <summary>
        /// The number of blocks to include in the query, must be at least 1.
        /// </summary>
        public uint NumberOfBlocks { get; set; }
    }
}
