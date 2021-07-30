using System;
using Lyn.Types.Serialization;
using System.Buffers;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class OpenChannelSerializer : IProtocolTypeSerializer<OpenChannel>
    {
        public int Serialize(OpenChannel typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            //writer.GetSpan(1024);

            size += writer.WriteUint256(typeInstance.ChainHash, true);
            size += writer.WriteUint256(typeInstance.TemporaryChannelId, true);
            size += writer.WriteULong(typeInstance.FundingSatoshis,true);
            size += writer.WriteULong(typeInstance.PushMsat,true);
            size += writer.WriteULong(typeInstance.DustLimitSatoshis,true);
            size += writer.WriteULong(typeInstance.MaxHtlcValueInFlightMsat,true);
            size += writer.WriteULong(typeInstance.ChannelReserveSatoshis,true);
            size += writer.WriteULong(typeInstance.HtlcMinimumMsat,true);
            size += writer.WriteUInt(typeInstance.FeeratePerKw,true);
            size += writer.WriteUShort(typeInstance.ToSelfDelay,true);
            size += writer.WriteUShort(typeInstance.MaxAcceptedHtlcs,true);
            size += writer.WriteBytes(typeInstance.FundingPubkey);
            size += writer.WriteBytes(typeInstance.RevocationBasepoint);
            size += writer.WriteBytes(typeInstance.PaymentBasepoint);
            size += writer.WriteBytes(typeInstance.DelayedPaymentBasepoint);
            size += writer.WriteBytes(typeInstance.HtlcBasepoint);
            size += writer.WriteBytes(typeInstance.FirstPerCommitmentPoint);
            size += writer.WriteByte(typeInstance.ChannelFlags);

            return size;
        }

        public OpenChannel Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var message = new OpenChannel();

            message.ChainHash = reader.ReadUint256(true);
            message.TemporaryChannelId = new (reader.ReadUint256(true).GetBytes().ToArray());
            message.FundingSatoshis = reader.ReadULong(true);
            message.PushMsat = reader.ReadULong(true);
            message.DustLimitSatoshis = reader.ReadULong(true);
            message.MaxHtlcValueInFlightMsat = reader.ReadULong(true);
            message.ChannelReserveSatoshis = reader.ReadULong(true);
            message.HtlcMinimumMsat = reader.ReadULong(true);
            message.FeeratePerKw = reader.ReadUInt(true);
            message.ToSelfDelay = reader.ReadUShort(true);
            message.MaxAcceptedHtlcs = reader.ReadUShort(true);
            message.FundingPubkey = reader.ReadBytes(PublicKey.LENGTH);
            message.RevocationBasepoint = reader.ReadBytes(PublicKey.LENGTH);
            message.PaymentBasepoint = reader.ReadBytes(PublicKey.LENGTH);
            message.DelayedPaymentBasepoint = reader.ReadBytes(PublicKey.LENGTH);
            message.HtlcBasepoint = reader.ReadBytes(PublicKey.LENGTH);
            message.FirstPerCommitmentPoint = reader.ReadBytes(PublicKey.LENGTH);
            message.ChannelFlags = reader.ReadByte();

            return message;
        }
    }
}