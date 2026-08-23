using System;
using System.Linq;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types.Fundamental;
using NBitcoin.Secp256k1;

namespace Lyn.Protocol.Bolt4
{
    /// <summary>
    /// Implements the shared secret generation and ephemeral key blinding from BOLT 4.
    /// </summary>
    public class SphinxSharedSecrets
    {
        /// <summary>
        /// Computes the shared secret for a hop: sha256(ecdh(ephemeral_privkey, hop_pubkey)).
        /// </summary>
        public static byte[] ComputeSharedSecret(ECPrivKey ephemeralPrivKey, ECPubKey hopPubKey)
        {
            var sharedPoint = hopPubKey.GetSharedPubkey(ephemeralPrivKey);

            var serialized = new byte[33];
            sharedPoint.WriteToSpan(true, serialized, out _);

            return HashGenerator.Sha256(serialized).ToArray();
        }

        /// <summary>
        /// The blinding factor is sha256(ephemeral_pubkey_compressed || shared_secret).
        /// </summary>
        public static byte[] ComputeBlindingFactor(ECPubKey ephemeralPubKey, byte[] sharedSecret)
        {
            var serialized = new byte[33];
            ephemeralPubKey.WriteToSpan(true, serialized, out _);

            var input = new byte[33 + sharedSecret.Length];
            serialized.CopyTo(input, 0);
            sharedSecret.CopyTo(input, 33);

            return HashGenerator.Sha256(input).ToArray();
        }

        /// <summary>
        /// Multiplies an ephemeral private key by the blinding factor to obtain the
        /// ephemeral key for the next hop.
        /// </summary>
        public static ECPrivKey BlindEphemeralPrivateKey(ECPrivKey current, ReadOnlySpan<byte> blindingFactor)
        {
            return current.TweakMul(blindingFactor);
        }
    }
}
