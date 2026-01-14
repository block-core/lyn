using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFulfillHtlcSerializer : IProtocolTypeSerializer<UpdateFulfillHtlc>
    {
        public int Serialize(UpdateFulfillHtlc typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = 0;

            bytesWritten += writer.WriteUint256(typeInstance.ChannelId!, false);
            bytesWritten += writer.WriteULong(typeInstance.Id ?? 0, true);
            bytesWritten += writer.WriteBytes(typeInstance.PaymentPreimage!.GetBytes().ToArray());

            return bytesWritten;
        }

        public UpdateFulfillHtlc Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            return new UpdateFulfillHtlc
            {
                ChannelId = reader.ReadUint256(),
                Id = reader.ReadULong(true),
                PaymentPreimage = new Preimage(reader.ReadBytes(32).ToArray())
            };
        }
    }
}

