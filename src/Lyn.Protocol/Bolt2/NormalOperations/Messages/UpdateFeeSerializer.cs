using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFeeSerializer : IProtocolTypeSerializer<UpdateFee>
    {
        public int Serialize(UpdateFee typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = 0;

            bytesWritten += writer.WriteUint256(typeInstance.ChannelId!, false);
            bytesWritten += writer.WriteUInt(typeInstance.FeeratePerKw ?? 0, true);

            return bytesWritten;
        }

        public UpdateFee Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            return new UpdateFee
            {
                ChannelId = reader.ReadUint256(),
                FeeratePerKw = reader.ReadUInt(true)
            };
        }
    }
}

