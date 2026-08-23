using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class QueryShortChannelIdsSerializer : IProtocolTypeSerializer<QueryShortChannelIds>
    {
        public QueryShortChannelIds Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var chainHash = reader.ReadUint256(false);
            ushort len = reader.ReadUShort(true);

            return new QueryShortChannelIds(
                chainHash, len, len > 0 ? reader.ReadBytes(len).ToArray() : new byte[0]);
        }

        public int Serialize(QueryShortChannelIds typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChainHash, false);
            size += writer.WriteUShort((ushort)(typeInstance.EncodedShortIds?.Length ?? 0), true);
            if (typeInstance.EncodedShortIds?.Length > 0)
                size += writer.WriteBytes(typeInstance.EncodedShortIds);

            return size;
        }
    }
}
