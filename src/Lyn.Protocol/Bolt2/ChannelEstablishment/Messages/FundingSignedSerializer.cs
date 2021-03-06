using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class FundingSignedSerializer : IProtocolTypeSerializer<FundingSigned>
    {
        public int Serialize(FundingSigned typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId, false);
            if (typeInstance.Signature != null)
            {
                size += writer.WriteBytes(typeInstance.Signature);
            }

            return size;
        }

        public FundingSigned Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new FundingSigned
            {
                ChannelId = reader.ReadUint256(false),
                Signature = reader.ReadBytes(CompressedSignature.LENGTH)
            };
        }
    }
}