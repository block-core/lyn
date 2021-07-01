using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class TransactionWitnessComponentSerializer : IProtocolTypeSerializer<TransactionWitnessComponent>
    {
        public TransactionWitnessComponent Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new TransactionWitnessComponent
            {
                RawData = reader.ReadByteArray()
            };
        }

        public int Serialize(TransactionWitnessComponent typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            return writer.WriteByteArray(typeInstance.RawData);
        }
    }
}