using System.Buffers;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Types.Serialization.Serializers
{
    public class PongMessageSerializer : IProtocolTypeSerializer<PongMessage>
    {
        public int Serialize(PongMessage typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteUShort(typeInstance.BytesLen, true);
            size += writer.WriteBytes(typeInstance.Ignored);

            return size;
        }

        public PongMessage Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            ushort bytesLen = reader.ReadUShort(true);

            return new PongMessage
            {
                BytesLen = bytesLen,
                Ignored = reader.ReadBytes(bytesLen).ToArray()
            };
        }
    }
}