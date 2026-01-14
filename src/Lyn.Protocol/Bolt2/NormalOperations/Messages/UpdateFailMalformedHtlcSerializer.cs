using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFailMalformedHtlcSerializer : IProtocolTypeSerializer<UpdateFailMalformedHtlc>
    {
        public int Serialize(UpdateFailMalformedHtlc typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = 0;

            bytesWritten += writer.WriteUint256(typeInstance.ChannelId!, false);
            bytesWritten += writer.WriteULong(typeInstance.Id ?? 0, true);
            bytesWritten += writer.WriteUint256(typeInstance.Sha256OfOnion!, false);
            bytesWritten += writer.WriteUShort(typeInstance.FailureCode ?? 0, true);

            return bytesWritten;
        }

        public UpdateFailMalformedHtlc Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            return new UpdateFailMalformedHtlc
            {
                ChannelId = reader.ReadUint256(),
                Id = reader.ReadULong(true),
                Sha256OfOnion = reader.ReadUint256(),
                FailureCode = reader.ReadUShort(true)
            };
        }
    }
}

