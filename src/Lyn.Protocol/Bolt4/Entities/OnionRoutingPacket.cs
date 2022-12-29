using Lyn.Types.Fundamental;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4.Entities
{
    public class OnionRoutingPacket
    {

        public byte Version { get; set; }

        public PublicKey EphemeralKey { get; set; }

        public byte[] PayloadData { get; set; } 

        public byte[] Hmac { get; set; }

        public static implicit operator ReadOnlySpan<byte>(OnionRoutingPacket packet)
        {
            var packetBytes = new List<byte>() { packet.Version };
            packetBytes.AddRange(packet.EphemeralKey.GetSpan().ToArray());
            packetBytes.AddRange(packet.PayloadData);
            packetBytes.AddRange(packet.Hmac);
            return packetBytes.ToArray();
        }

        public static implicit operator OnionRoutingPacket(ReadOnlySpan<byte> packetData)
        {
            var parsedPacket = new OnionRoutingPacket();
            // note: fixed sphinx header sizes
            var payloadLength = packetData.Length - 66;
            parsedPacket.Version = packetData[0];
            parsedPacket.EphemeralKey = new PublicKey(packetData.Slice(1, 32).ToArray());
            parsedPacket.PayloadData = packetData.Slice(33, payloadLength).ToArray();
            parsedPacket.Hmac = packetData.Slice(payloadLength, 32).ToArray();
            return parsedPacket;
        }
    }
}
