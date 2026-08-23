using System;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class ReplyChannelRange : GossipMessage
    {
        public ReplyChannelRange()
        {
            ChainHash = ChainHashes.Bitcoin;
            EncodedShortIds = new byte[0];
        }

        public ReplyChannelRange(UInt256 chainHash, uint firstBlocknum, uint numberOfBlocks, bool complete, byte[] encodedShortIds)
        {
            ChainHash = chainHash;
            FirstBlocknum = firstBlocknum;
            NumberOfBlocks = numberOfBlocks;
            Complete = complete;
            EncodedShortIds = encodedShortIds;
        }

        public override MessageType MessageType => MessageType.ReplyChannelRange;

        public UInt256 ChainHash { get; set; }

        /// <summary>
        /// The first block in the range of blocks the reply covers, must be greater than or
        /// equal to the value from the query.
        /// </summary>
        public uint FirstBlocknum { get; set; }

        /// <summary>
        /// The number of blocks covered by this reply.
        /// </summary>
        public uint NumberOfBlocks { get; set; }

        /// <summary>
        /// Whether the querying node has all channel announcements and updates within the
        /// requested range (1) or not (0).
        /// </summary>
        public bool Complete { get; set; }

        /// <summary>
        /// The encoded short channel ids covering the range (see BOLT 7 for the encoding).
        /// </summary>
        public ushort Len { get; set; }

        public byte[] EncodedShortIds { get; set; }
    }
}
