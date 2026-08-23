using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class QueryChannelRangeSerializer : IProtocolTypeSerializer<QueryChannelRange>
    {
        public QueryChannelRange Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new QueryChannelRange
            {
                ChainHash = reader.ReadUint256(false),
                FirstBlocknum = reader.ReadUInt(true),
                NumberOfBlocks = reader.ReadUInt(true)
            };
        }

        public int Serialize(QueryChannelRange typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChainHash, false);
            size += writer.WriteUInt(typeInstance.FirstBlocknum, true);
            size += writer.WriteUInt(typeInstance.NumberOfBlocks, true);

            return size;
        }
    }
}
