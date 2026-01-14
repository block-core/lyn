using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt7.Messages
{
    /// <summary>
    /// Reply to query_channel_range with a list of short_channel_ids.
    /// </summary>
    public class ReplyChannelRange : GossipMessage
    {
        public ReplyChannelRange()
        {
            MessageType = MessageType.ReplyChannelRange;
        }

        public override MessageType MessageType { get; }

        /// <summary>
        /// The genesis hash of the chain being queried.
        /// </summary>
        public UInt256? ChainHash { get; set; }

        /// <summary>
        /// The first block height in this reply.
        /// </summary>
        public uint FirstBlocknum { get; set; }

        /// <summary>
        /// The number of blocks covered in this reply.
        /// </summary>
        public uint NumberOfBlocks { get; set; }

        /// <summary>
        /// Indicates if the query is complete (1) or if more replies follow (0).
        /// </summary>
        public byte Complete { get; set; }

        /// <summary>
        /// Length of the encoded short_channel_ids.
        /// </summary>
        public ushort Len { get; set; }

        /// <summary>
        /// Encoded short_channel_ids (first byte is encoding type).
        /// </summary>
        public byte[]? EncodedShortIds { get; set; }
    }
}

