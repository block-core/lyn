using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class OutPointSerializer : IProtocolTypeSerializer<OutPoint>
    {
        public OutPoint Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new OutPoint
            {
                Hash = reader.ReadUint256(),
                Index = reader.ReadUInt()
            };
        }

        public int Serialize(OutPoint typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int size = writer.WriteUint256(typeInstance.Hash);
            size += writer.WriteUInt(typeInstance.Index);

            return size;
        }
    }
}