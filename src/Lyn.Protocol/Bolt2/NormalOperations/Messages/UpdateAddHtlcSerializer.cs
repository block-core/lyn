using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateAddHtlcSerializer : IProtocolTypeSerializer<UpdateAddHtlc>
    {
        public const ushort ONION_ROUTING_PACKET_LENGTH = 1366;

        public UpdateAddHtlc Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new UpdateAddHtlc
            {
                ChannelId = reader.ReadUint256(false),
                Id = reader.ReadULong(true),
                AmountMsat = reader.ReadULong(true),
                PaymentHash = reader.ReadUint256(false),
                CltvExpiry = reader.ReadUInt(true),
                OnionRoutingPacket = reader.ReadBytes(ONION_ROUTING_PACKET_LENGTH).ToArray()
            };
        }

        public int Serialize(UpdateAddHtlc typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId ?? new UInt256(new byte[32]), false);
            size += writer.WriteULong(typeInstance.Id ?? 0, true);
            size += writer.WriteULong(typeInstance.AmountMsat ?? 0, true);
            size += writer.WriteUint256(typeInstance.PaymentHash ?? new UInt256(new byte[32]), false);
            size += writer.WriteUInt(typeInstance.CltvExpiry ?? 0, true);
            size += writer.WriteBytes(typeInstance.OnionRoutingPacket ?? new byte[ONION_ROUTING_PACKET_LENGTH]);

            return size;
        }
    }
}
