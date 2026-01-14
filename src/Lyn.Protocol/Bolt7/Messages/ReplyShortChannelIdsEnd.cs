using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt7.Messages
{
    /// <summary>
    /// Indicates the end of replies to query_short_channel_ids.
    /// </summary>
    public class ReplyShortChannelIdsEnd : GossipMessage
    {
        public ReplyShortChannelIdsEnd()
        {
            MessageType = MessageType.ReplyShortChannelIdsEnd;
        }

        public override MessageType MessageType { get; }

        /// <summary>
        /// The genesis hash of the chain being queried.
        /// </summary>
        public UInt256? ChainHash { get; set; }

        /// <summary>
        /// Indicates if the query is complete.
        /// 1 = complete (all channels returned)
        /// 0 = incomplete (some channels may be missing)
        /// </summary>
        public byte Complete { get; set; }
    }
}

