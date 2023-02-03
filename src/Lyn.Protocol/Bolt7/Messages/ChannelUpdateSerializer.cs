using System.Buffers;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class ChannelUpdateSerializer : IProtocolTypeSerializer<ChannelUpdate>
    {
        public int Serialize(ChannelUpdate typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int size = 0;

            size += writer.WriteBytes(typeInstance.Signature);
            size += writer.WriteUint256(typeInstance.ChainHash, true);
            size += writer.WriteBytes(typeInstance.ShortChannelId);
            size += writer.WriteUInt(typeInstance.TimeStamp,true);
            size += writer.WriteByte(typeInstance.MessageFlags);
            size += writer.WriteByte(typeInstance.ChannelFlags);
            size += writer.WriteUShort(typeInstance.CltvExpiryDelta,true);
            size += writer.WriteULong(typeInstance.HtlcMinimumMsat,true);
            size += writer.WriteUInt(typeInstance.FeeBaseMsat,true);
            size += writer.WriteUInt(typeInstance.FeeProportionalMillionths,true);
            size += writer.WriteULong(typeInstance.HtlcMaximumMsat,true);

            return size;
        }

        public ChannelUpdate Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new ChannelUpdate
            {
                Signature = reader.ReadBytes(CompressedSignature.LENGTH),
                ChainHash = reader.ReadUint256(true),
                ShortChannelId = reader.ReadBytes(ShortChannelId.LENGTH),
                TimeStamp = reader.ReadUInt(true),
                MessageFlags = reader.ReadByte(),
                ChannelFlags = reader.ReadByte(),
                CltvExpiryDelta = reader.ReadUShort(true),
                HtlcMinimumMsat = reader.ReadULong(true),
                FeeBaseMsat = reader.ReadUInt(true),
                FeeProportionalMillionths = reader.ReadUInt(true),
                HtlcMaximumMsat = reader.ReadULong(true)
            };
        }
    }
}