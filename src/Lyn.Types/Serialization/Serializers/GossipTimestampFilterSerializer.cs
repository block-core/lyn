using System;
using System.Buffers;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Types.Serialization.Serializers
{
    public class GossipTimestampFilterSerializer : IProtocolTypeSerializer<GossipTimestampFilter>
    {
        public int Serialize(GossipTimestampFilter typeInstance, int protocolVersion, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteBytes(typeInstance.ChainHash ?? throw new ArgumentNullException(nameof(typeInstance.ChainHash)));
            size += writer.WriteUInt(typeInstance.FirstTimestamp);
            size += writer.WriteUInt(typeInstance.TimestampRange);

            return size;
        }

        public GossipTimestampFilter Deserialize(ref SequenceReader<byte> reader, int protocolVersion,
            ProtocolTypeSerializerOptions? options = null)
        {
            return new GossipTimestampFilter
            {
                ChainHash = (ChainHash) reader.ReadBytes(32),
                FirstTimestamp = reader.ReadUInt(),
                TimestampRange = reader.ReadUInt()
            };
        }
    }
}