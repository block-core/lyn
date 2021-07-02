using Lyn.Types.Fundamental;

namespace Lyn.Types.Bolt.Messages
{
    public class AnnouncementSignatures : GossipMessage
    {
        private const string COMMAND = "259";

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

        public override string Command => COMMAND;

        public ChannelId ChannelId { get; set; }

        public ShortChannelId ShortChannelId { get; set; }

        public CompressedSignature NodeSignature { get; set; }

        public CompressedSignature BitcoinSignature { get; set; }
    }
}