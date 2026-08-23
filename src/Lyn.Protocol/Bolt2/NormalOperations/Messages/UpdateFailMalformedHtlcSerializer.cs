using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFailMalformedHtlcSerializer : IProtocolTypeSerializer<UpdateFailMalformedHtlc>
    {
        public UpdateFailMalformedHtlc Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new UpdateFailMalformedHtlc
            {
                ChannelId = reader.ReadUint256(false),
                Id = reader.ReadULong(true),
                Sha256OfOnion = reader.ReadUint256(false),
                FailureCode = reader.ReadUShort(true)
            };
        }

        public int Serialize(UpdateFailMalformedHtlc typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId ?? new UInt256(new byte[32]), false);
            size += writer.WriteULong(typeInstance.Id ?? 0, true);
            size += writer.WriteUint256(typeInstance.Sha256OfOnion ?? new UInt256(new byte[32]), false);
            size += writer.WriteUShort(typeInstance.FailureCode ?? 0, true);

            return size;
        }
    }
}
