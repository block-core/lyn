using System;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class ReplyShortChannelIdsEnd : GossipMessage
    {
        public ReplyShortChannelIdsEnd()
        {
            ChainHash = ChainHashes.Bitcoin;
        }

        public ReplyShortChannelIdsEnd(UInt256 chainHash, bool complete)
        {
            ChainHash = chainHash;
            Complete = complete;
        }

        public override MessageType MessageType => MessageType.ReplyShortChannelIdsEnd;

        public UInt256 ChainHash { get; set; }

        /// <summary>
        /// Whether the responding node has complete information for the queried
        /// short channel ids (1) or not (0).
        /// </summary>
        public bool Complete { get; set; }
    }
}
