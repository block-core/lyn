using System;
using System.Security.Cryptography;

namespace Lyn.Protocol.Bolt4
{
    /// <summary>
    /// Implements the key generation and pseudo random byte stream from BOLT 4.
    /// </summary>
    public static class SphinxKeys
    {
        public const int SHARED_SECRET_LENGTH = 32;
        public const int KEY_LENGTH = 32;
        public const int NONCE_LENGTH = 12;

        // Key types as defined by BOLT 4, no null termination is used.
        public const string RHO = "rho";
        public const string MU = "mu";
        public const string UM = "um";
        public const string PAD = "pad";
        public const string AMMAG = "ammag";
        public const string AMMAGEXT = "ammagext";

        /// <summary>
        /// Generates a 32-byte key from a key-type and a 32-byte secret by
        /// computing HMAC-SHA256 with the key-type as HMAC-key and the secret as message.
        /// </summary>
        public static byte[] GenerateKey(string keyType, ReadOnlySpan<byte> sharedSecret)
        {
            using var hmac = new HMACSHA256(System.Text.Encoding.ASCII.GetBytes(keyType));

            Span<byte> result = stackalloc byte[32];
            if (!hmac.TryComputeHash(sharedSecret, result, out _))
                throw new InvalidOperationException("Failed to generate sphinx key");

            return result.ToArray();
        }

        /// <summary>
        /// Generates a pseudo random byte stream of the requested length by encrypting an
        /// all zero plain text with ChaCha20 using the provided key and a 96-bit zero nonce.
        /// </summary>
        public static byte[] GenerateCipherStream(byte[] key, int length)
        {
            var chacha = new NaCl.Core.ChaCha20(key, 0);

            Span<byte> nonce = stackalloc byte[NONCE_LENGTH];
            nonce.Clear();

            var plainText = new byte[length];
            var cipherText = new byte[length];

            chacha.Encrypt(plainText.AsSpan(), nonce, cipherText.AsSpan());

            return cipherText;
        }
    }
}
