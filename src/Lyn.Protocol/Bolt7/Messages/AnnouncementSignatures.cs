using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class AnnouncementSignatures : GossipMessage
    {

        public AnnouncementSignatures(ChannelId channelId, ShortChannelId shortChannelId, CompressedSignature nodeSignature, CompressedSignature bitcoinSignature)
        {
            ChannelId = channelId;
            ShortChannelId = shortChannelId;
            NodeSignature = nodeSignature;
            BitcoinSignature = bitcoinSignature;
        }

        public AnnouncementSignatures()
        {
            ChannelId = new ChannelId(new byte[] { 0 });
            ShortChannelId = new ShortChannelId(new byte[8]);
            NodeSignature = new CompressedSignature();
            BitcoinSignature = new CompressedSignature();
        }

        public override MessageType MessageType => MessageType.AnnouncementSignatures;

        public ChannelId ChannelId { get; set; }

        public ShortChannelId ShortChannelId { get; set; }

        public CompressedSignature NodeSignature { get; set; }

        public CompressedSignature BitcoinSignature { get; set; }
    }
}