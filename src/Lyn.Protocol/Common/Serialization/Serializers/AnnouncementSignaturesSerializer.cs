using System.Buffers;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Types.Serialization.Serializers
{
    public class AnnouncementSignaturesSerializer : IProtocolTypeSerializer<AnnouncementSignatures>
    {
        public int Serialize(AnnouncementSignatures message, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteBytes(message.ChannelId);
            size += writer.WriteBytes(message.ShortChannelId);
            size += writer.WriteBytes(message.NodeSignature);
            size += writer.WriteBytes(message.BitcoinSignature);

            return size;
        }

        public AnnouncementSignatures Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            return new AnnouncementSignatures
            {
                ChannelId = reader.ReadBytes(32),
                ShortChannelId = reader.ReadBytes(8),
                NodeSignature = reader.ReadBytes(CompressedSignature.LENGTH),
                BitcoinSignature = reader.ReadBytes(CompressedSignature.LENGTH),
            };
        }
    }
}