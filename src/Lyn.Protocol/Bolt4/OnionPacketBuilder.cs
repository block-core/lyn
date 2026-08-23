using System;
using System.Collections.Generic;
using System.Linq;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types.Fundamental;
using NBitcoin.Secp256k1;

namespace Lyn.Protocol.Bolt4
{
    /// <summary>
    /// Per-hop data used during packet construction.
    /// </summary>
    public class SphinxHop
    {
        public byte[] Payload { get; set; } = new byte[0];

        public byte[] Hmac { get; set; } = new byte[OnionPacket.HMAC_LENGTH];
    }

    /// <summary>
    /// Implements the sphinx packet construction from BOLT 4 (Packet Construction section).
    /// </summary>
    public class OnionPacketBuilder
    {
        private const int NUM_MAX_HOPS = 20;

        /// <summary>
        /// Creates an onion packet for the given route.
        /// </summary>
        /// <param name="sessionKey">the random session key used as first ephemeral key</param>
        /// <param name="hopPublicKeys">public keys of every hop along the route</param>
        /// <param name="hopsPayloads">payload for each hop, in route order</param>
        /// <param name="associatedData">data committed to by all HMACs (e.g. payment_hash)</param>
        public static OnionPacket CreateOnionPacket(
            PrivateKey sessionKey,
            PublicKey[] hopPublicKeys,
            byte[][] hopsPayloads,
            byte[]? associatedData = null)
        {
            associatedData ??= Array.Empty<byte>();

            if (hopPublicKeys.Length != hopsPayloads.Length)
                throw new ArgumentException("A payload must be provided for each hop");

            if (hopPublicKeys.Length == 0 || hopPublicKeys.Length > NUM_MAX_HOPS)
                throw new ArgumentException($"Route must contain between 1 and {NUM_MAX_HOPS} hops");

            if (!ECPrivKey.TryCreate(sessionKey, Context.Instance, out ECPrivKey? ephemeralKey) ||
                ephemeralKey is null)
                throw new ArgumentException("Invalid session key", nameof(sessionKey));

            // Compute shared secret and blinded ephemeral key for each hop.
            var sharedSecrets = new byte[hopPublicKeys.Length][];
            var ephemeralPubKeys = new ECPubKey[hopPublicKeys.Length];

            for (int i = 0; i < hopPublicKeys.Length; i++)
            {
                if (!ECPubKey.TryCreate(hopPublicKeys[i], Context.Instance, out _, out ECPubKey? hopPubKey) ||
                    hopPubKey is null)
                    throw new ArgumentException($"Hop {i} has an invalid public key");

                sharedSecrets[i] = SphinxSharedSecrets.ComputeSharedSecret(ephemeralKey, hopPubKey);
                ephemeralPubKeys[i] = ephemeralKey.CreatePubKey();

                byte[] blindingFactor =
                    SphinxSharedSecrets.ComputeBlindingFactor(ephemeralPubKeys[i], sharedSecrets[i]);

                ephemeralKey = SphinxSharedSecrets.BlindEphemeralPrivateKey(ephemeralKey, blindingFactor);
            }

            int numHops = hopPublicKeys.Length;

            // Compute the shift size of each hop: bigsize(payload_length) + payload_length + hmac.
            int[] shiftSizes = hopsPayloads
                .Select(p => BigSizeLength((ulong)p.Length) + p.Length + OnionPacket.HMAC_LENGTH)
                .ToArray();

            // Generate the filler.
            byte[] filler = GenerateFiller(numHops, shiftSizes, sharedSecrets);
            // Initialize the mix header with pseudo random bytes derived from the session key.
            var mixHeader = SphinxKeys.GenerateCipherStream(
                SphinxKeys.GenerateKey(SphinxKeys.PAD, sessionKey), OnionPacket.HOP_PAYLOADS_LENGTH);

            var nextHmac = new byte[OnionPacket.HMAC_LENGTH];

            var hops = hopsPayloads.Select(p => new SphinxHop { Payload = p }).ToArray();

            for (int i = numHops - 1; i >= 0; i--)
            {
                byte[] rhoKey = SphinxKeys.GenerateKey(SphinxKeys.RHO, sharedSecrets[i]);
                byte[] muKey = SphinxKeys.GenerateKey(SphinxKeys.MU, sharedSecrets[i]);

                hops[i].Hmac = nextHmac;

                // Shift and obfuscate routing information.
                byte[] streamBytes = SphinxKeys.GenerateCipherStream(rhoKey, OnionPacket.HOP_PAYLOADS_LENGTH);

                RightShift(mixHeader, shiftSizes[i]);

                var hopBytes = new byte[shiftSizes[i]];
                WriteBigSize(hopBytes.AsSpan(), (ulong)hops[i].Payload.Length);

                int offset = BigSizeLength((ulong)hops[i].Payload.Length);
                hops[i].Payload.CopyTo(hopBytes, offset);
                hops[i].Hmac.CopyTo(hopBytes, offset + hops[i].Payload.Length);

                Array.Copy(hopBytes, 0, mixHeader, 0, shiftSizes[i]);

                Xor(mixHeader, streamBytes);

                if (i == numHops - 1)
                {
                    // Overwrite the tail with the pre-generated filler so that every node
                    // sees a correctly padded packet.
                    filler.CopyTo(mixHeader, OnionPacket.HOP_PAYLOADS_LENGTH - filler.Length);
                }

                // Compute the hmac for this hop over the mix header and the associated data.
                var macInput = new byte[OnionPacket.HOP_PAYLOADS_LENGTH + associatedData.Length];
                mixHeader.CopyTo(macInput, 0);
                associatedData.CopyTo(macInput, OnionPacket.HOP_PAYLOADS_LENGTH);

                nextHmac = HmacSha256(muKey, macInput);
            }

            return new OnionPacket
            {
                Version = OnionPacket.VERSION,
                PublicKey = ephemeralPubKeys[0].ToBytes(),
                HopPayloads = mixHeader,
                Hmac = nextHmac
            };
        }

        /// <summary>
        /// Generates the deterministic padding that each intermediate hop will generate when
        /// forwarding so the origin node can compute correct HMACs.
        ///
        /// In route order each hop appends shift-size zero bytes to the filler and XORs the
        /// result with the tail of that hop's rho stream, see BOLT 4 Filler Generation.
        /// </summary>
        private static byte[] GenerateFiller(int numHops, int[] shiftSizes, byte[][] sharedSecrets)
        {
            int fillerSize = 0;

            for (int i = 0; i < numHops - 1; i++)
                fillerSize += shiftSizes[i];

            if (fillerSize == 0)
                return Array.Empty<byte>();

            var filler = new byte[fillerSize];
            int previousLength = 0;

            for (int i = 0; i < numHops - 1; i++)
            {
                byte[] rhoKey = SphinxKeys.GenerateKey(SphinxKeys.RHO, sharedSecrets[i]);

                int streamLength = OnionPacket.HOP_PAYLOADS_LENGTH + shiftSizes[i];
                byte[] stream = SphinxKeys.GenerateCipherStream(rhoKey, streamLength);

                int newLength = previousLength + shiftSizes[i];

                // XOR the existing filler bytes with the corresponding stream window,
                // which ends at 1300 + shift_size.
                int streamStart = streamLength - newLength;

                for (int k = 0; k < previousLength; k++)
                    filler[k] ^= stream[streamStart + k];

                // The newly appended region is the continuation of the stream past 1300 bytes,
                // exactly what the next hop will see appended after de-obfuscation.
                for (int t = 0; t < shiftSizes[i]; t++)
                    filler[previousLength + t] = stream[OnionPacket.HOP_PAYLOADS_LENGTH + t];

                previousLength = newLength;
            }

            return filler;
        }

        internal static byte[] HmacSha256(byte[] key, ReadOnlySpan<byte> data)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(key);

            Span<byte> result = stackalloc byte[32];
            if (!hmac.TryComputeHash(data, result, out _))
                throw new InvalidOperationException("Failed to compute hmac");

            return result.ToArray();
        }

        internal static void Xor(Span<byte> target, ReadOnlySpan<byte> other)
        {
            for (int i = 0; i < target.Length; i++)
                target[i] ^= other[i];
        }

        internal static void RightShift(Span<byte> target, int count)
        {
            if (count <= 0)
                return;

            for (int i = target.Length - 1; i >= count; i--)
                target[i] = target[i - count];

            target.Slice(0, count).Clear();
        }

        internal static int BigSizeLength(ulong value)
        {
            if (value < 0xFD)
                return 1;
            if (value < 0x10000)
                return 3;
            if (value < 0x100000000)
                return 5;

            return 9;
        }

        internal static void WriteBigSize(Span<byte> span, ulong value)
        {
            if (value < 0xFD)
            {
                span[0] = (byte)value;
            }
            else if (value < 0x10000)
            {
                span[0] = 0xFD;
                span[1] = (byte)(value >> 8);
                span[2] = (byte)value;
            }
            else if (value < 0x100000000)
            {
                span[0] = 0xFE;
                span[1] = (byte)(value >> 24);
                span[2] = (byte)(value >> 16);
                span[3] = (byte)(value >> 8);
                span[4] = (byte)value;
            }
            else
            {
                span[0] = 0xFF;
                for (int i = 0; i < 8; i++)
                    span[1 + i] = (byte)(value >> (56 - i * 8));
            }
        }
    }
}
