using System;
using Lyn.Types.Serialization;
using System.Buffers;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class OpenChannelSerializer : IProtocolTypeSerializer<OpenChannel>
    {
        public int Serialize(OpenChannel typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteBytes(typeInstance.ChainHash);
            size += writer.WriteBytes(typeInstance.TemporaryChannelId);
            size += writer.WriteULong(typeInstance.FundingSatoshis);
            size += writer.WriteULong(typeInstance.PushMsat);
            size += writer.WriteULong(typeInstance.DustLimitSatoshis);
            size += writer.WriteULong(typeInstance.MaxHtlcValueInFlightMsat);
            size += writer.WriteULong(typeInstance.ChannelReserveSatoshis);
            size += writer.WriteULong(typeInstance.HtlcMinimumMsat);
            size += writer.WriteUShort(typeInstance.ToSelfDelay);
            size += writer.WriteUShort(typeInstance.MaxAcceptedHtlcs);
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

            message.ChainHash = reader.ReadBytes(32);
            message.TemporaryChannelId = reader.ReadBytes(32);
            message.FundingSatoshis = reader.ReadULong();
            message.PushMsat = reader.ReadULong();
            message.DustLimitSatoshis = reader.ReadULong();
            message.MaxHtlcValueInFlightMsat = reader.ReadULong();
            message.ChannelReserveSatoshis = reader.ReadULong();
            message.HtlcMinimumMsat = reader.ReadULong();
            message.ToSelfDelay = reader.ReadUShort();
            message.MaxAcceptedHtlcs = reader.ReadUShort();
            message.FundingPubkey = reader.ReadBytes(32);
            message.RevocationBasepoint = reader.ReadBytes(32);
            message.PaymentBasepoint = reader.ReadBytes(32);
            message.DelayedPaymentBasepoint = reader.ReadBytes(32);
            message.HtlcBasepoint = reader.ReadBytes(32);
            message.FirstPerCommitmentPoint = reader.ReadBytes(32);
            message.ChannelFlags = reader.ReadByte();

            return message;
        }
    }
}