using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class ReplyShortChannelIdsEndSerializer : IProtocolTypeSerializer<ReplyShortChannelIdsEnd>
    {
        public ReplyShortChannelIdsEnd Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new ReplyShortChannelIdsEnd
            {
                ChainHash = reader.ReadUint256(false),
                Complete = reader.ReadBool()
            };
        }

        public int Serialize(ReplyShortChannelIdsEnd typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChainHash, false);
            size += writer.WriteBool(typeInstance.Complete);

            return size;
        }
    }
}
