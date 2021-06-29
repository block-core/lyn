using Lyn.Types.Serialization;
using System.Buffers;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class AcceptChannelSerializer : IProtocolTypeSerializer<AcceptChannel>
    {
        public int Serialize(AcceptChannel typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteBytes(typeInstance.TemporaryChannelId);
            size += writer.WriteULong(typeInstance.DustLimitSatoshis);
            size += writer.WriteULong(typeInstance.MaxHtlcValueInFlightMsat);
            size += writer.WriteULong(typeInstance.ChannelReserveSatoshis);
            size += writer.WriteULong(typeInstance.HtlcMinimumMsat);
            size += writer.WriteUInt(typeInstance.MinimumDepth);
            size += writer.WriteUShort(typeInstance.ToSelfDelay);
            size += writer.WriteUShort(typeInstance.MaxAcceptedHtlcs);
            size += writer.WriteBytes(typeInstance.FundingPubkey);
            size += writer.WriteBytes(typeInstance.RevocationBasepoint);
            size += writer.WriteBytes(typeInstance.PaymentBasepoint);
            size += writer.WriteBytes(typeInstance.DelayedPaymentBasepoint);
            size += writer.WriteBytes(typeInstance.HtlcBasepoint);
            size += writer.WriteBytes(typeInstance.FirstPerCommitmentPoint);

            return size;
        }

        public AcceptChannel Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var message = new AcceptChannel();

            message.TemporaryChannelId = reader.ReadBytes(32);
            message.DustLimitSatoshis = reader.ReadULong();
            message.MaxHtlcValueInFlightMsat = reader.ReadULong();
            message.ChannelReserveSatoshis = reader.ReadULong();
            message.HtlcMinimumMsat = reader.ReadULong();
            message.MinimumDepth = reader.ReadUInt();
            message.ToSelfDelay = reader.ReadUShort();
            message.MaxAcceptedHtlcs = reader.ReadUShort();
            message.FundingPubkey = reader.ReadBytes(32);
            message.RevocationBasepoint = reader.ReadBytes(32);
            message.PaymentBasepoint = reader.ReadBytes(32);
            message.DelayedPaymentBasepoint = reader.ReadBytes(32);
            message.HtlcBasepoint = reader.ReadBytes(32);
            message.FirstPerCommitmentPoint = reader.ReadBytes(32);

            return message;
        }
    }
}