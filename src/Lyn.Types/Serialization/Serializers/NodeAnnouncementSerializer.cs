using System.Buffers;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Types.Serialization.Serializers
{
    public class NodeAnnouncementSerializer : IProtocolTypeSerializer<NodeAnnouncement>
    {
        public int Serialize(NodeAnnouncement typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteBytes(typeInstance.Signature);
            size += writer.WriteUShort(typeInstance.Len, true);
            size += writer.WriteBytes(typeInstance.Features);
            size += writer.WriteUInt(typeInstance.Timestamp, true);
            size += writer.WriteBytes(typeInstance.NodeId);
            size += writer.WriteBytes(typeInstance.RgbColor);
            size += writer.WriteBytes(typeInstance.Alias);
            size += writer.WriteUShort(typeInstance.Addrlen, true);
            size += writer.WriteBytes(typeInstance.Addresses);

            return size;
        }

        public NodeAnnouncement Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            var message = new NodeAnnouncement
            {
                Signature = (CompressedSignature)reader.ReadBytes(CompressedSignature.LENGTH),
                Len = reader.ReadUShort(true)
            };

            message.Features = reader.ReadBytes(message.Len).ToArray();
            message.Timestamp = reader.ReadUInt(true);
            message.NodeId = (PublicKey)reader.ReadBytes(PublicKey.LENGTH);
            message.RgbColor = reader.ReadBytes(3).ToArray();
            message.Alias = reader.ReadBytes(32).ToArray();
            message.Addrlen = reader.ReadUShort(true);
            message.Addresses = reader.ReadBytes(message.Addrlen).ToArray();

            return message;
        }
    }
}