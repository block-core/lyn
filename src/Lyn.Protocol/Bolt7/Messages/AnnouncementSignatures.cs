using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class AnnouncementSignatures : GossipMessage
    {

        public AnnouncementSignatures(UInt256 channelId, ShortChannelId shortChannelId, CompressedSignature nodeSignature, CompressedSignature bitcoinSignature)
        {
            ChannelId = channelId;
            ShortChannelId = shortChannelId;
            NodeSignature = nodeSignature;
            BitcoinSignature = bitcoinSignature;
        }

        public AnnouncementSignatures()
        {
            ChannelId = new UInt256(new byte[32]);
            ShortChannelId = new ShortChannelId(new byte[8]);
            NodeSignature = new CompressedSignature();
            BitcoinSignature = new CompressedSignature();
        }

        public override MessageType MessageType => MessageType.AnnouncementSignatures;

        public UInt256 ChannelId { get; set; }

        public ShortChannelId ShortChannelId { get; set; }

        public CompressedSignature NodeSignature { get; set; }

        public CompressedSignature BitcoinSignature { get; set; }
    }
}