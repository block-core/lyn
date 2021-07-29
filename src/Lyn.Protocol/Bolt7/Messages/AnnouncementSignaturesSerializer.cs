using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class AnnouncementSignaturesSerializer : IProtocolTypeSerializer<AnnouncementSignatures>
    {
        public int Serialize(AnnouncementSignatures message, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteUint256(message.ChannelId,true);
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
                ChannelId = new UInt256(reader.ReadUint256(true).GetBytes().ToArray()),
                ShortChannelId = reader.ReadBytes(8),
                NodeSignature = reader.ReadBytes(CompressedSignature.LENGTH),
                BitcoinSignature = reader.ReadBytes(CompressedSignature.LENGTH),
            };
        }
    }
}