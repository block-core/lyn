using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages
{
    public class ClosingSignedSerializer : IProtocolTypeSerializer<ClosingSigned>
    {
        public ClosingSigned Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new ClosingSigned
            {
                ChannelId = reader.ReadUint256(false),
                FeeSatoshis = reader.ReadULong(true),
                Signature = new CompressedSignature(reader.ReadBytes(CompressedSignature.LENGTH).ToArray())
            };
        }

        public int Serialize(ClosingSigned typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId ?? new UInt256(new byte[32]), false);
            size += writer.WriteULong(typeInstance.FeeSatoshis ?? 0, true);
            if (typeInstance.Signature != null)
                size += writer.WriteBytes(typeInstance.Signature);

            return size;
        }
    }
}
