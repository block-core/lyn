using System.Buffers;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt1.Messages
{
    public class PingMessageSerializer : IProtocolTypeSerializer<PingMessage>
    {
        public int Serialize(PingMessage typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteUShort(typeInstance.NumPongBytes, true);
            size += writer.WriteUShort(typeInstance.BytesLen, true);
            size += writer.WriteBytes(typeInstance.Ignored);

            return size;
        }

        public PingMessage Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var numPongBytes = reader.ReadUShort(true);
            var bytesLen = reader.ReadUShort(true);

            return new PingMessage
            {
                NumPongBytes = numPongBytes,
                BytesLen = bytesLen,
                Ignored = reader.ReadBytes(bytesLen).ToArray()
            };
        }
    }
}