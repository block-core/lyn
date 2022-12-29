using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4.Entities
{
    public class OnionRoutingPacketSerializer : IProtocolTypeSerializer<OnionRoutingPacket>
    {
        public OnionRoutingPacket Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var packet = new OnionRoutingPacket();

            var verByte = reader.ReadByte();
            packet.Version = verByte;

            var onionKeyBytes = reader.ReadBytes(33);
            packet.EphemeralKey = new PublicKey(onionKeyBytes.ToArray());

            // note: maybe this is a safe cast? need to confirm in spec
            // The Sphinx packet header contains a version (1 byte), a public key (33 bytes) and a mac (32 bytes) -> total 66 bytes
            var payloadLength = (int)reader.Length - 66;
            var payloadBytes = reader.ReadBytes(payloadLength);

            packet.PayloadData = payloadBytes.ToArray();

            var hmacBytes = reader.ReadBytes(32);
            packet.Hmac = hmacBytes.ToArray();

            return packet;
        }

        public int Serialize(OnionRoutingPacket typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int bytesWritten = 0;

            bytesWritten += writer.WriteByte(typeInstance.Version);
            bytesWritten += writer.WriteBytes(typeInstance.EphemeralKey);
            bytesWritten += writer.WriteBytes(typeInstance.PayloadData);
            bytesWritten += writer.WriteBytes(typeInstance.Hmac);

            return bytesWritten;
        }
    }
}
