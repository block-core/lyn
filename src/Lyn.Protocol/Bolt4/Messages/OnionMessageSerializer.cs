using Lyn.Protocol.Bolt4.Entities;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4.Messages
{
    public class OnionMessageSerializer : IProtocolTypeSerializer<OnionMessage>
    {
        private readonly IProtocolTypeSerializer<OnionRoutingPacket> packetSerializer;

        public OnionMessageSerializer(IProtocolTypeSerializer<OnionRoutingPacket> _packetSerializer)
        {
            this.packetSerializer = _packetSerializer;
        }

        public OnionMessage Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var onionMessage = new OnionMessage();

            var blindingKeyBytes = reader.ReadBytes(33);
            onionMessage.BlindingKey = new PublicKey(blindingKeyBytes.ToArray());

            // read the onion packet data from the message
            onionMessage.OnionPacket = packetSerializer.Deserialize(ref reader, options);

            return onionMessage;
        }

        public int Serialize(OnionMessage typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int bytesWritten = 0;

            bytesWritten += writer.WriteBytes(typeInstance.BlindingKey);
            bytesWritten += packetSerializer.Serialize(typeInstance.OnionPacket, writer, options);    

            return bytesWritten;
        }
    }
}
