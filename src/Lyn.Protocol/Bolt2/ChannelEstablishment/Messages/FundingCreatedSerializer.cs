using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class FundingCreatedSerializer : IProtocolTypeSerializer<FundingCreated>
    {
        public int Serialize(FundingCreated typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.TemporaryChannelId, true);
            size += writer.WriteUint256(typeInstance.FundingTxid);
            size += writer.WriteUShort((ushort)typeInstance.FundingOutputIndex, true);

            if (typeInstance.Signature != null)
            {
                size += writer.WriteBytes(typeInstance.Signature);
            }

            return size;
        }

        public FundingCreated Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new FundingCreated
            {
                TemporaryChannelId = reader.ReadUint256(true),
                FundingTxid = reader.ReadUint256(),
                FundingOutputIndex = reader.ReadUShort(),
                Signature = reader.ReadBytes(CompressedSignature.LENGTH)
            };
        }
    }
}