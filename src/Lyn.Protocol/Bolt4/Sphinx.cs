using Lyn.Protocol.Common.Crypto;
using Lyn.Types.Fundamental;
using NaCl.Core;
using NBitcoin.Secp256k1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4
{
    public class Sphinx : ISphinx
    {
        private readonly IEllipticCurveActions _ellipticCurveActions;

        public Sphinx(IEllipticCurveActions ellipticCurveActions)
        {
            _ellipticCurveActions = ellipticCurveActions;
        }


        public ReadOnlySpan<byte> ComputeSharedSecret(PublicKey publicKey, PrivateKey secret)
        {
            return HashGenerator.Sha256(_ellipticCurveActions.MultiplyPubKey(secret, publicKey));
        }

        public ReadOnlySpan<byte> GenerateSphinxKey(byte[] keyType, ReadOnlySpan<byte> secret)
        {
            return HashGenerator.HmacSha256(keyType, secret);
        }

        public ReadOnlySpan<byte> GenerateSphinxKey(string keyType, ReadOnlySpan<byte> secret)
        {
            var keyTypeBytes = Encoding.UTF8.GetBytes(keyType);
            return GenerateSphinxKey(keyTypeBytes, secret);
        }

        public PrivateKey DeriveBlindedPrivateKey(PrivateKey privateKey, PublicKey blindingEphemeralKey)
        {
            var sharedSecret = ComputeSharedSecret(blindingEphemeralKey, privateKey);
            var generatedKey = GenerateSphinxKey("blinded_node_id", sharedSecret);
            var newKeyBytes = _ellipticCurveActions.MultiplyPubKey(privateKey, generatedKey);
            return new PrivateKey(newKeyBytes.ToArray());
        }

        // todo: util/helper
        public ReadOnlySpan<byte> ExclusiveOR(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            if (left.Length != right.Length)
                throw new ArgumentException("inputs must be same length");

            byte[] result = new byte[left.Length];

            for (int i = 0; i < left.Length; i++)
            {
                result[i] = (byte)(left[i] ^ right[i]);
            }

            return result;
        }

        public ReadOnlySpan<byte> GenerateStream(ReadOnlyMemory<byte> keyData, int streamLength)
        {
            var cipher = new ChaCha20(keyData, 0);
            var emptyPlainText = Enumerable.Range(0, streamLength).Select<int, byte>(x => 0x00).ToArray();
            var nonce = Enumerable.Range(0, 12).Select<int, byte>(x => 0x00).ToArray();
            return cipher.Encrypt(emptyPlainText, nonce);
        }

        public ReadOnlySpan<byte> ComputeBlindingFactor(PublicKey pubKey, ReadOnlySpan<byte> secret)
        {
            // welcome to allocation city baby - population: BlindingFactor
            return HashGenerator.Sha256(pubKey.GetSpan().ToArray().Concat(secret.ToArray()).ToArray());
        }

        public PublicKey BlindKey(PublicKey pubKey, ReadOnlySpan<byte> blindingFactor)
        {
            var blindKeyBytes = _ellipticCurveActions.MultiplyPubKey(new PrivateKey(blindingFactor.ToArray()), pubKey);
            return new PublicKey(blindKeyBytes.ToArray());
        }

        public PublicKey BlindKey(PublicKey pubKey, IEnumerable<byte[]> blindingFactors)
        {
            return blindingFactors.Aggregate(pubKey, (key, blindingFactor) => BlindKey(key, blindingFactor));
        }

        public (IEnumerable<PublicKey>, IEnumerable<byte[]>) ComputeEphemeralPublicKeysAndSharedSecrets(PrivateKey sessionKey,
                                                                                                        ICollection<PublicKey> publicKeys)
        {
            // this seems inelegant as fuck?
            var key = new ECPubKey(EC.G, null);
            var ephemeralPublicKey0 = BlindKey(new PublicKey(key.ToBytes()), sessionKey);
            var secret0 = ComputeSharedSecret(publicKeys.First(), sessionKey);
            var blindingFactor0 = ComputeBlindingFactor(ephemeralPublicKey0, secret0);

            IList<PublicKey> ephemeralPublicKeys = new List<PublicKey> { ephemeralPublicKey0 };
            IList<byte[]> blindingFactors = new List<byte[]> { blindingFactor0.ToArray() };
            IList<byte[]> sharedSecrets = new List<byte[]>() { secret0.ToArray() };

            return ComputeEphemeralPublicKeysAndSharedSecrets(sessionKey,
                                                              publicKeys.Skip(1).ToList(),
                                                              ephemeralPublicKeys,
                                                              blindingFactors,
                                                              sharedSecrets);
        }

        public (IEnumerable<PublicKey>, IEnumerable<byte[]>) ComputeEphemeralPublicKeysAndSharedSecrets(PrivateKey sessionKey,
                                                                                                        ICollection<PublicKey> publicKeys,
                                                                                                        IList<PublicKey> ephemeralPublicKeys,
                                                                                                        IList<byte[]> blindingFactors,
                                                                                                        IList<byte[]> sharedSecrets)
        {
            if (publicKeys.Count == 0)
            {
                return (ephemeralPublicKeys, sharedSecrets);
            }
            else
            {
                var ephemeralPublicKey = BlindKey(ephemeralPublicKeys.Last(), blindingFactors.Last());
                var secret = ComputeSharedSecret(BlindKey(publicKeys.First(), blindingFactors), sessionKey);
                var blindingFactor = ComputeBlindingFactor(ephemeralPublicKey, secret);

                ephemeralPublicKeys.Add(ephemeralPublicKey);
                blindingFactors.Add(blindingFactor.ToArray());
                sharedSecrets.Add(secret.ToArray());

                return ComputeEphemeralPublicKeysAndSharedSecrets(sessionKey,
                                                              publicKeys.Skip(1).ToList(),
                                                              ephemeralPublicKeys,
                                                              blindingFactors,
                                                              sharedSecrets);
            }
        }


    }
}
