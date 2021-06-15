using System.Buffers;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;
using ShortChannelId = Lyn.Types.Bolt.ShortChannelId;

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
                ChannelId = (ChannelId)reader.ReadBytes(32).ToArray(),
                ShortChannelId = (ShortChannelId)reader.ReadBytes(8).ToArray(),
                NodeSignature = (CompressedSignature)reader.ReadBytes(CompressedSignature.LENGTH).ToArray(),
                BitcoinSignature = (CompressedSignature)reader.ReadBytes(CompressedSignature.LENGTH).ToArray(),
            };
        }
    }
}