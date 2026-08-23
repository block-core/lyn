using System;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt4
{
    /// <summary>
    /// The onion packet as transferred in the onion_routing_packet field of
    /// update_add_htlc and in onion messages (BOLT 4).
    ///
    /// 1. type: onion_packet
    /// 2. data:
    ///    * [`byte`:`version`] (0x00)
    ///    * [`33*byte`:`public_key`]
    ///    * [`1300*byte`:`hop_payloads`]
    ///    * [`32*byte`:`hmac`]
    /// </summary>
    public class OnionPacket
    {
        public const int VERSION = 0;
        public const int PUBLIC_KEY_LENGTH = 33;
        public const int HOP_PAYLOADS_LENGTH = 1300;
        public const int HMAC_LENGTH = 32;
        public const int PACKET_LENGTH = 1 + PUBLIC_KEY_LENGTH + HOP_PAYLOADS_LENGTH + HMAC_LENGTH;

        public byte Version { get; set; } = VERSION;

        public PublicKey PublicKey { get; set; } = new(new byte[PUBLIC_KEY_LENGTH]);

        public byte[] HopPayloads { get; set; } = new byte[HOP_PAYLOADS_LENGTH];

        public byte[] Hmac { get; set; } = new byte[HMAC_LENGTH];

        public static OnionPacket Parse(byte[] data)
        {
            if (data == null || data.Length != PACKET_LENGTH)
                throw new ArgumentException($"onion packet must be {PACKET_LENGTH} bytes", nameof(data));

            return new OnionPacket
            {
                Version = data[0],
                PublicKey = data.AsSpan(1, PUBLIC_KEY_LENGTH).ToArray(),
                HopPayloads = data.AsSpan(1 + PUBLIC_KEY_LENGTH, HOP_PAYLOADS_LENGTH).ToArray(),
                Hmac = data.AsSpan(1 + PUBLIC_KEY_LENGTH + HOP_PAYLOADS_LENGTH, HMAC_LENGTH).ToArray()
            };
        }

        public byte[] ToByteArray()
        {
            var result = new byte[PACKET_LENGTH];

            result[0] = Version;
            ((ReadOnlySpan<byte>)PublicKey.GetSpan()).CopyTo(result.AsSpan(1));
            HopPayloads.CopyTo(result, 1 + PUBLIC_KEY_LENGTH);
            Hmac.CopyTo(result, 1 + PUBLIC_KEY_LENGTH + HOP_PAYLOADS_LENGTH);

            return result;
        }
    }
}
