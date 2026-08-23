using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFeeSerializer : IProtocolTypeSerializer<UpdateFee>
    {
        public UpdateFee Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new UpdateFee
            {
                ChannelId = reader.ReadUint256(false),
                FeeratePerKw = reader.ReadUInt(true)
            };
        }

        public int Serialize(UpdateFee typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId ?? new UInt256(new byte[32]), false);
            size += writer.WriteUInt(typeInstance.FeeratePerKw ?? 0, true);

            return size;
        }
    }
}
