using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class TransactionInputSerializer : IProtocolTypeSerializer<TransactionInput>
    {
        private readonly IProtocolTypeSerializer<OutPoint> _outPointSerializator;

        public TransactionInputSerializer(IProtocolTypeSerializer<OutPoint> outPointSerializator)
        {
            _outPointSerializator = outPointSerializator;
        }

        public TransactionInput Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new TransactionInput
            {
                PreviousOutput = reader.ReadWithSerializer(_outPointSerializator),
                SignatureScript = reader.ReadByteArray(),
                Sequence = reader.ReadUInt()
            };
        }

        public int Serialize(TransactionInput typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int size = writer.WriteWithSerializer(typeInstance.PreviousOutput!, _outPointSerializator);
            size += writer.WriteByteArray(typeInstance.SignatureScript);
            size += writer.WriteUInt(typeInstance.Sequence);

            return size;
        }
    }
}