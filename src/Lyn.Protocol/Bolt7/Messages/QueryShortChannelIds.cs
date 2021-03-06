using System;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class QueryShortChannelIds : GossipMessage
    {
        public QueryShortChannelIds()
        {
            ChainHash = ChainHashes.Bitcoin;
            Len = 0;
            EncodedShortIds = new Byte[0];
        }

        public QueryShortChannelIds(UInt256 chainHash, ushort len, byte[] encodedShortIds)
        {
            ChainHash = chainHash;
            Len = len;
            EncodedShortIds = encodedShortIds;
        }

        public override MessageType MessageType => MessageType.QueryShortChannelIds;

        public UInt256 ChainHash { get; set; }

        public ushort Len { get; set; }

        public byte[] EncodedShortIds { get; set; }
    }
}