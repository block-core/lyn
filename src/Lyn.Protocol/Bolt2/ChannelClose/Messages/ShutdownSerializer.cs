using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages
{
    public class ShutdownSerializer : IProtocolTypeSerializer<Shutdown>
    {
        public Shutdown Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var channelId = reader.ReadUint256(false);
            ushort length = reader.ReadUShort(true);

            return new Shutdown
            {
                ChannelId = channelId,
                Length = length,
                ScriptPubkey = length > 0 ? reader.ReadBytes(length).ToArray() : new byte[0]
            };
        }

        public int Serialize(Shutdown typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId ?? new UInt256(new byte[32]), false);
            size += writer.WriteUShort((ushort)(typeInstance.ScriptPubkey?.Length ?? 0), true);
            if (typeInstance.ScriptPubkey?.Length > 0)
                size += writer.WriteBytes(typeInstance.ScriptPubkey);

            return size;
        }
    }
}
