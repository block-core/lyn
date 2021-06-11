using System.Buffers;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Types.Serialization.Serializers
{
    public class ChannelAnnouncementSerializer : IProtocolTypeSerializer<ChannelAnnouncement>
    {
        public int Serialize(ChannelAnnouncement typeInstance, int protocolVersion, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteBytes(typeInstance.NodeSignature1);
            size += writer.WriteBytes(typeInstance.NodeSignature2);
            size += writer.WriteBytes(typeInstance.BitcoinSignature1);
            size += writer.WriteBytes(typeInstance.BitcoinSignature2);
            size += writer.WriteUShort(typeInstance.Len);
            size += writer.WriteBytes(typeInstance.Features);
            size += writer.WriteBytes(typeInstance.ChainHash);
            size += writer.WriteBytes(typeInstance.ShortChannelId);
            size += writer.WriteBytes(typeInstance.NodeId1);
            size += writer.WriteBytes(typeInstance.NodeId2);
            size += writer.WriteBytes(typeInstance.BitcoinKey1);
            size += writer.WriteBytes(typeInstance.BitcoinKey2);

            return size;
        }

        public ChannelAnnouncement Deserialize(ref SequenceReader<byte> reader, int protocolVersion,
            ProtocolTypeSerializerOptions? options = null)
        {
            var message = new ChannelAnnouncement
            {
                NodeSignature1 = (CompressedSignature)reader.ReadBytes(CompressedSignature.LENGTH),
                NodeSignature2 = (CompressedSignature)reader.ReadBytes(CompressedSignature.LENGTH),
                BitcoinSignature1 = (CompressedSignature)reader.ReadBytes(CompressedSignature.LENGTH),
                BitcoinSignature2 = (CompressedSignature)reader.ReadBytes(CompressedSignature.LENGTH),
            };

            message.Len = reader.ReadUShort();
            message.Features = reader.ReadBytes(message.Len).ToArray();
            message.ChainHash = (ChainHash) reader.ReadBytes(32);
            message.ShortChannelId = (ShortChannelId)reader.ReadBytes(8).ToArray();
            message.NodeId1 = (PublicKey)reader.ReadBytes(PublicKey.LENGTH);
            message.NodeId2 = (PublicKey)reader.ReadBytes(PublicKey.LENGTH);
            message.BitcoinKey1 = (PublicKey)reader.ReadBytes(PublicKey.LENGTH);
            message.BitcoinKey2 = (PublicKey)reader.ReadBytes(PublicKey.LENGTH);
         
            return message;
        }
    }
}