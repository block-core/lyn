using System;
using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class UInt256Serializer : IProtocolTypeSerializer<UInt256>
    {
        public UInt256 Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
        {
            return new UInt256(reader.ReadBytes(32));
        }

        public int Serialize(UInt256 typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            ReadOnlySpan<byte> span = typeInstance.GetBytes();
            writer.Write(span);
            return span.Length;
        }
    }
}