using System;

namespace Lyn.Types.Bolt.Messages
{
    public class QueryShortChannelIds : GossipMessage
    {
        private const string COMMAND = "261";

        public QueryShortChannelIds()
        {
            ChainHash = ChainHashes.Bitcoin;
            Len = 0;
            EncodedShortIds = new Byte[0];
        }

        public QueryShortChannelIds(ChainHash chainHash, ushort len, byte[] encodedShortIds)
        {
            ChainHash = chainHash;
            Len = len;
            EncodedShortIds = encodedShortIds;
        }

        public override string Command => COMMAND;

        public ChainHash ChainHash { get; set; }

        public ushort Len { get; set; }

        public byte[] EncodedShortIds { get; set; }
    }
}