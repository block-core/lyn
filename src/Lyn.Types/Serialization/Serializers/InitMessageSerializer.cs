using System.Buffers;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Types.Serialization.Serializers
{
    public class InitMessageSerializer : IProtocolTypeSerializer<InitMessage>
    {
        public int Serialize(InitMessage typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteUShort((ushort)typeInstance.GlobalFeatures.Length, true);
            writer.WriteBytes(typeInstance.GlobalFeatures);

            size += typeInstance.GlobalFeatures.Length;

            size += writer.WriteUShort((ushort)typeInstance.Features.Length, true);
            writer.WriteBytes(typeInstance.Features);

            size += typeInstance.Features.Length;

            return size;
        }

        public InitMessage Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var message = new InitMessage();

            ushort len = reader.ReadUShort(true);
            message.GlobalFeatures = reader.ReadBytes(len).ToArray();

            len = reader.ReadUShort(true);
            message.Features = reader.ReadBytes(len).ToArray();

            return message;
        }
    }
}