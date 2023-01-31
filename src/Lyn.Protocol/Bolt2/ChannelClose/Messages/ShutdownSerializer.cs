using System.Buffers;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages
{
    public class ShutdownSerializer : IProtocolTypeSerializer<Shutdown>
    {
        public int Serialize(Shutdown typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId, true);
            size += writer.WriteUShort(typeInstance.Length ?? 0, true);
            size += writer.WriteBytes(typeInstance.ScriptPubkey);

            return size;
        }

        public Shutdown Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var message = new Shutdown();
            
            message.ChannelId = reader.ReadUint256();
            
            var length = reader.ReadUShort(true);
            message.Length = length;
            
            message.ScriptPubkey = reader.ReadBytes(length).ToArray();
            
            return message;
        }
    }
}