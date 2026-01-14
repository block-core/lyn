using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class ReplyShortChannelIdsEndSerializer : IProtocolTypeSerializer<ReplyShortChannelIdsEnd>
    {
        public int Serialize(ReplyShortChannelIdsEnd typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = 0;

            bytesWritten += writer.WriteUint256(typeInstance.ChainHash!, false);
            bytesWritten += writer.WriteByte(typeInstance.Complete);

            return bytesWritten;
        }

        public ReplyShortChannelIdsEnd Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            return new ReplyShortChannelIdsEnd
            {
                ChainHash = reader.ReadUint256(),
                Complete = reader.ReadByte()
            };
        }
    }
}

