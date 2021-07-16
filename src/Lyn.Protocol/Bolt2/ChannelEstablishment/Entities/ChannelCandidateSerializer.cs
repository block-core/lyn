using System.Buffers;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Entities
{
    public class ChannelCandidateSerializer : IProtocolTypeSerializer<ChannelCandidate>
    {
        private readonly IProtocolTypeSerializer<OpenChannel> _openChannelSerializer;
        private readonly IProtocolTypeSerializer<AcceptChannel> _acceptChannelSerializer;

        public ChannelCandidateSerializer(
            IProtocolTypeSerializer<OpenChannel> openChannelSerializer,
            IProtocolTypeSerializer<AcceptChannel> acceptChannelSerializer)
        {
            _openChannelSerializer = openChannelSerializer;
            _acceptChannelSerializer = acceptChannelSerializer;
        }

        public int Serialize(ChannelCandidate typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            writer.WriteByte((byte)typeInstance.ChannelOpener);

            if (typeInstance.OpenChannel == null) return size;
            size += _openChannelSerializer.Serialize(typeInstance.OpenChannel, writer, options);
            size += writer.WriteByteArray(typeInstance.OpenChannelUpfrontShutdownScript);

            if (typeInstance.AcceptChannel == null) return size;
            size += _acceptChannelSerializer.Serialize(typeInstance.AcceptChannel, writer, options);
            size += writer.WriteByteArray(typeInstance.AcceptChannelUpfrontShutdownScript);

            return size;
        }

        public ChannelCandidate Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var message = new ChannelCandidate();

            message.ChannelOpener = (ChannelSide)reader.ReadByte();

            if (reader.Length == 0) return message;
            message.OpenChannel = _openChannelSerializer.Deserialize(ref reader, options);
            message.OpenChannelUpfrontShutdownScript = reader.ReadByteArray();

            if (reader.Length == 0) return message;
            message.AcceptChannel = _acceptChannelSerializer.Deserialize(ref reader, options);
            message.AcceptChannelUpfrontShutdownScript = reader.ReadByteArray();

            return message;
        }
    }
}