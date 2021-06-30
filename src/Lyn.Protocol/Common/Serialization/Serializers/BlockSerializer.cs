using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class BlockSerializer : IProtocolTypeSerializer<Block>
    {
        private readonly IProtocolTypeSerializer<BlockHeader> _blockHeaderSerializer;
        private readonly IProtocolTypeSerializer<Transaction> _transactionSerializer;

        public BlockSerializer(IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer, IProtocolTypeSerializer<Transaction> transactionSerializer)
        {
            _blockHeaderSerializer = blockHeaderSerializer;
            _transactionSerializer = transactionSerializer;
        }

        public Block Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            options = (options ?? new ProtocolTypeSerializerOptions())
               .Set(SerializerOptions.HEADER_IN_BLOCK, false);

            return new Block
            {
                Header = reader.ReadWithSerializer(_blockHeaderSerializer, options),
                Transactions = reader.ReadArray(_transactionSerializer, options)
            };
        }

        public int Serialize(Block typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            options = (options ?? new ProtocolTypeSerializerOptions())
               .Set(SerializerOptions.HEADER_IN_BLOCK, false);

            int size = 0;
            size += writer.WriteWithSerializer(typeInstance.Header!, _blockHeaderSerializer, options);
            size += writer.WriteArray(typeInstance.Transactions!, _transactionSerializer, options);

            return size;
        }
    }
}