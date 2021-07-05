using Lyn.Protocol.Bolt7.Messages;

namespace Lyn.Protocol.Tests.Bolt7
{
    public class RandomGossipMessages
    {
        internal AnnouncementSignatures NewAnnouncementSignatures()
        {
            return new AnnouncementSignatures(RandomMessages.NewRandomChannelId(),
                RandomMessages.NewRandomShortChannelId(),
                RandomMessages.NewRandomCompressedSignature(),
                RandomMessages.NewRandomCompressedSignature());
        }

        internal ChannelAnnouncement NewChannelAnnouncement()
        {
            return new()
            {
                BitcoinSignature1 = RandomMessages.NewRandomCompressedSignature(),
                BitcoinSignature2 = RandomMessages.NewRandomCompressedSignature(),
                ShortChannelId = RandomMessages.NewRandomShortChannelId(),
                NodeSignature1 = RandomMessages.NewRandomCompressedSignature(),
                NodeSignature2 = RandomMessages.NewRandomCompressedSignature(),
                BitcoinKey1 = RandomMessages.NewRandomPublicKey(),
                BitcoinKey2 = RandomMessages.NewRandomPublicKey(),
                NodeId1 = RandomMessages.NewRandomPublicKey(),
                NodeId2 = RandomMessages.NewRandomPublicKey(),
                ChainHash = RandomMessages.NewRandomChainHash()
            };
        }

        internal NodeAnnouncement NewNodeAnnouncement()
        {
            return new NodeAnnouncement
            {
                Addresses = RandomMessages.GetRandomByteArray(64),
                Addrlen = 64,
                Alias = RandomMessages.GetRandomByteArray(64),
                Signature = RandomMessages.NewRandomCompressedSignature(),
                NodeId = RandomMessages.NewRandomPublicKey(),
                Timestamp = RandomMessages.GetRandomNumberUInt32(),
                Features = new byte[0],
                Len = 0,
                RgbColor = new byte[0]
            };
        }
    }
}