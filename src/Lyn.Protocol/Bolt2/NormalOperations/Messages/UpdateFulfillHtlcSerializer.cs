using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFulfillHtlcSerializer : IProtocolTypeSerializer<UpdateFulfillHtlc>
    {
        public UpdateFulfillHtlc Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new UpdateFulfillHtlc
            {
                ChannelId = reader.ReadUint256(false),
                Id = reader.ReadULong(true),
                PaymentPreimage = new Preimage(reader.ReadBytes(32).ToArray())
            };
        }

        public int Serialize(UpdateFulfillHtlc typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId ?? new UInt256(new byte[32]), false);
            size += writer.WriteULong(typeInstance.Id ?? 0, true);
            size += writer.WriteBytes(typeInstance.PaymentPreimage ?? new Preimage(new byte[32]));

            return size;
        }
    }
}
