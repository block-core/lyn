using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class TransactionWitnessSerializer : IProtocolTypeSerializer<TransactionWitness>
    {
        private readonly IProtocolTypeSerializer<TransactionWitnessComponent> _transactionWitnessComponentSerializer;

        public TransactionWitnessSerializer(IProtocolTypeSerializer<TransactionWitnessComponent> transactionWitnessComponentSerializer)
        {
            _transactionWitnessComponentSerializer = transactionWitnessComponentSerializer;
        }

        public TransactionWitness Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new TransactionWitness
            {
                Components = reader.ReadArray(_transactionWitnessComponentSerializer)
            };
        }

        public int Serialize(TransactionWitness typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            return writer.WriteArray(typeInstance.Components, _transactionWitnessComponentSerializer);
        }
    }
}