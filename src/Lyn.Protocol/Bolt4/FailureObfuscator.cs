using System;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt4
{
    /// <summary>
    /// Implements the return path for onion errors from BOLT 4.
    ///
    /// The erring node builds a return packet:
    ///   * [`32*byte`:`hmac`]
    ///   * [`u16`:`failure_len`]
    ///   * [`failure_len*byte`:`failuremsg`]
    ///   * [`u16`:`pad_len`]
    ///   * [`pad_len*byte`:`pad`]
    ///
    /// and obfuscates it with the ammag key derived from its shared secret. Each
    /// intermediate hop obfuscates it again with its own ammag key. The origin node
    /// peels each layer with its stored shared secrets and identifies the failing hop
    /// with the um key.
    /// </summary>
    public static class FailureObfuscator
    {
        private const int HMAC_LENGTH = 32;
        private const int MIN_TOTAL_LENGTH = 256;

        /// <summary>
        /// Creates the obfuscated error packet at the erring node.
        /// </summary>
        public static byte[] CreateErrorPacket(byte[] sharedSecret, byte[] failureMessage, byte[]? pad = null)
        {
            pad ??= Array.Empty<byte>();

            // MUST set pad such that failure_len plus pad_len is at least 256.
            int paddingLength = pad.Length;

            if (failureMessage.Length + paddingLength < MIN_TOTAL_LENGTH)
                paddingLength = MIN_TOTAL_LENGTH - failureMessage.Length;

            var returnPacket = new byte[HMAC_LENGTH + 2 + failureMessage.Length + 2 + paddingLength];

            int offset = HMAC_LENGTH;

            returnPacket[offset++] = (byte)((failureMessage.Length >> 8) & 0xFF);
            returnPacket[offset++] = (byte)(failureMessage.Length & 0xFF);

            failureMessage.CopyTo(returnPacket, offset);
            offset += failureMessage.Length;

            returnPacket[offset++] = (byte)((paddingLength >> 8) & 0xFF);
            returnPacket[offset++] = (byte)(paddingLength & 0xFF);

            if (paddingLength > 0)
                Array.Copy(pad, 0, returnPacket, offset, Math.Min(pad.Length, paddingLength));

            byte[] umKey = SphinxKeys.GenerateKey(SphinxKeys.UM, sharedSecret);

            byte[] hmac = OnionPacketBuilder.HmacSha256(umKey, returnPacket.AsSpan(HMAC_LENGTH));
            hmac.CopyTo(returnPacket, 0);

            return Obfuscate(sharedSecret, returnPacket);
        }

        /// <summary>
        /// Applies one round of obfuscation with the shared secret of the current hop.
        /// Used by intermediate nodes to forward an error towards the origin node.
        /// </summary>
        public static byte[] Obfuscate(byte[] sharedSecret, byte[] errorPacket)
        {
            byte[] ammagKey = SphinxKeys.GenerateKey(SphinxKeys.AMMAG, sharedSecret);
            byte[] stream = SphinxKeys.GenerateCipherStream(ammagKey, errorPacket.Length);

            var result = new byte[errorPacket.Length];
            Array.Copy(errorPacket, result, errorPacket.Length);
            OnionPacketBuilder.Xor(result.AsSpan(), stream);

            return result;
        }

        /// <summary>
        /// Removes one layer of obfuscation using the shared secret of a hop on the route,
        /// returning true when this hop was the source of the failure message.
        /// </summary>
        public static bool Peel(byte[] sharedSecret, ref byte[] errorPacket, out byte[]? failureMessage)
        {
            byte[] deobfuscated = Obfuscate(sharedSecret, errorPacket);

            byte[] umKey = SphinxKeys.GenerateKey(SphinxKeys.UM, sharedSecret);

            if (deobfuscated.Length > HMAC_LENGTH)
            {
                byte[] expectedHmac =
                    OnionPacketBuilder.HmacSha256(umKey, deobfuscated.AsSpan(HMAC_LENGTH));

                byte[] actualHmac = new byte[HMAC_LENGTH];
                Array.Copy(deobfuscated, actualHmac, HMAC_LENGTH);

                if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedHmac, actualHmac))
                {
                    failureMessage = null;
                    errorPacket = deobfuscated;

                    return false;
                }

                int failureLen = (deobfuscated[HMAC_LENGTH] << 8) | deobfuscated[HMAC_LENGTH + 1];
                int failureOffset = HMAC_LENGTH + 2;

                if (failureOffset + failureLen <= deobfuscated.Length)
                {
                    failureMessage = new byte[failureLen];
                    Array.Copy(deobfuscated, failureOffset, failureMessage, 0, failureLen);
                    errorPacket = deobfuscated;

                    return true;
                }
            }

            failureMessage = null;
            errorPacket = deobfuscated;

            return false;
        }
    }
}
