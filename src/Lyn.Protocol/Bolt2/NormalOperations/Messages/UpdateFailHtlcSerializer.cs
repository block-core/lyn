using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFailHtlcSerializer : IProtocolTypeSerializer<UpdateFailHtlc>
    {
        public UpdateFailHtlc Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var channelId = reader.ReadUint256(false);
            ulong id = reader.ReadULong(true);
            ushort length = reader.ReadUShort(true);

            return new UpdateFailHtlc
            {
                ChannelId = channelId,
                Id = id,
                Length = length,
                Reason = length > 0 ? reader.ReadBytes(length).ToArray() : new byte[0]
            };
        }

        public int Serialize(UpdateFailHtlc typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId ?? new UInt256(new byte[32]), false);
            size += writer.WriteULong(typeInstance.Id ?? 0, true);
            size += writer.WriteUShort((ushort)(typeInstance.Reason?.Length ?? 0), true);
            if (typeInstance.Reason?.Length > 0)
                size += writer.WriteBytes(typeInstance.Reason);

            return size;
        }
    }
}
