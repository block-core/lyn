using System;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types.Fundamental;
using NBitcoin.Secp256k1;

namespace Lyn.Protocol.Bolt4
{
    /// <summary>
    /// The result of processing an onion packet at a hop.
    /// </summary>
    public class ProcessedOnionPacket
    {
        /// <summary>
        /// The TLV payload addressed to the current hop.
        /// </summary>
        public byte[] Payload { get; set; } = new byte[0];

        /// <summary>
        /// True when this hop is the final destination of the packet (all-zero next hmac).
        /// </summary>
        public bool IsFinalNode { get; set; }

        /// <summary>
        /// The obfuscated packet to forward to the next peer, only set when not the final node.
        /// </summary>
        public OnionPacket? ForwardPacket { get; set; }
    }

    /// <summary>
    /// Implements onion decryption from BOLT 4.
    /// </summary>
    public class OnionPacketProcessor
    {
        /// <summary>
        /// Verifies and peels one layer off the onion packet using the node private key.
        /// </summary>
        /// <exception cref="SphinxException">thrown when the packet is invalid or the hmac check fails</exception>
        public ProcessedOnionPacket ProcessOnionPacket(OnionPacket packet, PrivateKey privateKey, byte[]? associatedData = null)
        {
            associatedData ??= Array.Empty<byte>();

            if (packet.Version != OnionPacket.VERSION)
                throw new SphinxException("Unsupported onion packet version");

            if (!ECPubKey.TryCreate(packet.PublicKey, Context.Instance, out _, out ECPubKey? ephemeralPubKey) ||
                ephemeralPubKey is null)
                throw new SphinxException("Invalid ephemeral public key");

            if (!ECPrivKey.TryCreate(privateKey, Context.Instance, out ECPrivKey? nodePrivKey) ||
                nodePrivKey is null)
                throw new SphinxException("Invalid node private key");

            // Derive the shared secret.
            byte[] sharedSecret = SphinxSharedSecrets.ComputeSharedSecret(nodePrivKey, ephemeralPubKey);

            // Verify the hmac with constant time comparison.
            byte[] muKey = SphinxKeys.GenerateKey(SphinxKeys.MU, sharedSecret);

            var macInput = new byte[OnionPacket.HOP_PAYLOADS_LENGTH + associatedData.Length];
            packet.HopPayloads.CopyTo(macInput, 0);
            associatedData.CopyTo(macInput, OnionPacket.HOP_PAYLOADS_LENGTH);

            byte[] expectedHmac = OnionPacketBuilder.HmacSha256(muKey, macInput);

            if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedHmac, packet.Hmac))
                throw new SphinxException("Invalid onion packet hmac");

            // Deobfuscate the hop payloads.
            byte[] rhoKey = SphinxKeys.GenerateKey(SphinxKeys.RHO, sharedSecret);

            var unwrapped = new byte[OnionPacket.HOP_PAYLOADS_LENGTH * 2];
            packet.HopPayloads.CopyTo(unwrapped, 0);

            byte[] streamBytes = SphinxKeys.GenerateCipherStream(rhoKey, unwrapped.Length);
            OnionPacketBuilder.Xor(unwrapped.AsSpan(), streamBytes);

            // Parse bigsize payload length.
            int position = 0;
            ulong payloadLength = ReadBigSize(unwrapped, ref position);

            if (payloadLength < 2)
                throw new SphinxException("Payload length must be at least 2 bytes");

            if ((int)payloadLength + position + OnionPacket.HMAC_LENGTH > unwrapped.Length)
                throw new SphinxException("Payload extends beyond the hop payloads");

            var payload = new byte[payloadLength];
            Array.Copy(unwrapped, position, payload, 0, (int)payloadLength);
            position += (int)payloadLength;

            // Read next hmac.
            var nextHmac = new byte[OnionPacket.HMAC_LENGTH];
            Array.Copy(unwrapped, position, nextHmac, 0, OnionPacket.HMAC_LENGTH);
            position += OnionPacket.HMAC_LENGTH;

            bool isFinalNode = IsAllZero(nextHmac);

            var result = new ProcessedOnionPacket
            {
                Payload = payload,
                IsFinalNode = isFinalNode
            };

            if (isFinalNode)
                return result;

            // Blind the ephemeral key for the next peer.
            byte[] blindingTweak = SphinxSharedSecrets.ComputeBlindingFactor(ephemeralPubKey, sharedSecret);

            if (!ephemeralPubKey.TryTweakMul(blindingTweak, out ECPubKey? blindedPubKey) || blindedPubKey is null)
                throw new SphinxException("Failed to blind ephemeral public key");

            result.ForwardPacket = new OnionPacket
            {
                Version = OnionPacket.VERSION,
                PublicKey = blindedPubKey.ToBytes(),
                // The remaining obfuscated stream, truncated back to the incoming size.
                HopPayloads = SliceRange(unwrapped, position, OnionPacket.HOP_PAYLOADS_LENGTH),
                Hmac = nextHmac
            };

            return result;
        }

        private static byte[] SliceRange(byte[] source, int offset, int length)
        {
            var result = new byte[length];
            Array.Copy(source, offset, result, 0, Math.Min(length, source.Length - offset));

            return result;
        }

        private static ulong ReadBigSize(byte[] data, ref int position)
        {
            byte firstByte = data[position++];

            switch (firstByte)
            {
                case 0xFD:
                    ushort u16 = (ushort)((data[position] << 8) | data[position + 1]);
                    position += 2;
                    return u16;

                case 0xFE:
                    uint u32 = (uint)((data[position] << 24) | (data[position + 1] << 16)
                                      | (data[position + 2] << 8) | data[position + 3]);
                    position += 4;
                    return u32;

                case 0xFF:
                    ulong value = 0;
                    for (int i = 0; i < 8; i++)
                        value = (value << 8) | data[position + i];
                    position += 8;
                    return value;

                default:
                    return firstByte;
            }
        }

        private static bool IsAllZero(byte[] data)
        {
            foreach (byte b in data)
            {
                if (b != 0)
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Thrown when an onion packet fails validation during processing.
    /// </summary>
    public class SphinxException : Exception
    {
        public SphinxException(string message) : base(message)
        {
        }
    }
}
