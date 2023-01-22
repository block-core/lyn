using System.Buffers;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages
{
    public class ClosingSignedSerializer : IProtocolTypeSerializer<ClosingSigned>
    {
        public int Serialize(ClosingSigned typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId, true);
            size += writer.WriteULong(typeInstance.FeeSatoshis,true);
            size += writer.WriteBytes(typeInstance.Signature);

            return size;
        }

        public ClosingSigned Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new ClosingSigned
            {
                ChannelId = reader.ReadUint256(true),
                FeeSatoshis = reader.ReadULong(true),
                Signature = reader.ReadBytes(CompressedSignature.LENGTH)
            };
        }
    }
}