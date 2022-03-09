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
    internal class OnionMessageSerializer : IProtocolTypeSerializer<OnionMessage>
    {
        public OnionMessage Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var onionMessage = new OnionMessage();

            var blindingKeyBytes = reader.ReadBytes(33);
            onionMessage.BlindingKey = new PublicKey(blindingKeyBytes.ToArray());

            // read the onion packet data from the message

            var verByte = reader.ReadByte();
            onionMessage.OnionPacket.Version = verByte;

            var onionKeyBytes = reader.ReadBytes(33);
            onionMessage.OnionPacket.EphemeralKey = new PublicKey(onionKeyBytes.ToArray());

            // does this even belong here?
            if(!reader.TryPeek(out var peekedLength))
            {
                // puke
            }

            var payloadData = reader.ReadBytes(payloadLength);
            onionMessage.OnionPacket.PayloadData = payloadData.ToArray();

            var hmacBytes = reader.ReadBytes(32);
            onionMessage.OnionPacket.Hmac = hmacBytes.ToArray();

            return onionMessage;
        }

        public int Serialize(OnionMessage onionMessage, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            throw new NotImplementedException();
        }
    }
}
