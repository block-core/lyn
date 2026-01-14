using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class QueryChannelRangeSerializer : IProtocolTypeSerializer<QueryChannelRange>
    {
        public int Serialize(QueryChannelRange typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = 0;

            bytesWritten += writer.WriteUint256(typeInstance.ChainHash!, false);
            bytesWritten += writer.WriteUInt(typeInstance.FirstBlocknum, true);
            bytesWritten += writer.WriteUInt(typeInstance.NumberOfBlocks, true);

            return bytesWritten;
        }

        public QueryChannelRange Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            return new QueryChannelRange
            {
                ChainHash = reader.ReadUint256(),
                FirstBlocknum = reader.ReadUInt(true),
                NumberOfBlocks = reader.ReadUInt(true)
            };
        }
    }
}

