using System;
using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class GossipTimestampFilterSerializer : IProtocolTypeSerializer<GossipTimestampFilter>
    {
        public int Serialize(GossipTimestampFilter typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteUint256(typeInstance.ChainHash ?? throw new ArgumentNullException(nameof(typeInstance.ChainHash)),true);
            size += writer.WriteUInt(typeInstance.FirstTimestamp);
            size += writer.WriteUInt(typeInstance.TimestampRange);

            return size;
        }

        public GossipTimestampFilter Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            return new GossipTimestampFilter
            {
                ChainHash = new UInt256(reader.ReadUint256(true).GetBytes().ToArray()),
                FirstTimestamp = reader.ReadUInt(),
                TimestampRange = reader.ReadUInt()
            };
        }
    }
}