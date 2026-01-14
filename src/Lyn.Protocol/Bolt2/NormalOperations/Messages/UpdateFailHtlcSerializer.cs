using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFailHtlcSerializer : IProtocolTypeSerializer<UpdateFailHtlc>
    {
        public int Serialize(UpdateFailHtlc typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = 0;

            bytesWritten += writer.WriteUint256(typeInstance.ChannelId!, false);
            bytesWritten += writer.WriteULong(typeInstance.Id ?? 0, true);
            bytesWritten += writer.WriteUShort(typeInstance.Length ?? 0, true);
            if (typeInstance.Reason != null && typeInstance.Reason.Length > 0)
            {
                bytesWritten += writer.WriteBytes(typeInstance.Reason);
            }

            return bytesWritten;
        }

        public UpdateFailHtlc Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            var channelId = reader.ReadUint256();
            var id = reader.ReadULong(true);
            var length = reader.ReadUShort(true);
            var reason = length > 0 ? reader.ReadBytes(length).ToArray() : null;

            return new UpdateFailHtlc
            {
                ChannelId = channelId,
                Id = id,
                Length = length,
                Reason = reason
            };
        }
    }
}

