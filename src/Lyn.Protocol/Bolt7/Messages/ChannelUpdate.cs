using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class ChannelUpdate : GossipMessage
    {
        public override MessageType MessageType => MessageType.ChannelUpdate;

        public CompressedSignature Signature { get; set; }
        public UInt256 ChainHash { get; set; }
        public ShortChannelId ShortChannelId { get; set; }
        public uint TimeStamp { get; set; }
        public byte MessageFlags { get; set; }
        public byte ChannelFlags { get; set; }
        public ushort CltvExpiryDelta { get; set; }
        public ulong HtlcMinimumMsat { get; set; }
        public uint FeeBaseMsat { get; set; }
        public uint FeeProportionalMillionths { get; set; }
        public ulong HtlcMaximumMsat { get; set; }
    }
}