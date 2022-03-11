using Lyn.Protocol.Bolt4.Entities;
using Lyn.Protocol.Common.Crypto;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;
using NaCl.Core;
using NBitcoin.Secp256k1;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4
{
    public class Sphinx : ISphinx
    {
        //todo: move somewhere better
        private const int MAC_LENGTH = 32;

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

        // TODO: return DecryptedOnionPacket?
        public DecryptedOnionPacket PeelOnion(PrivateKey privateKey, byte[]? associatedData, OnionRoutingPacket packet)
        {

            var sharedSecret = ComputeSharedSecret(packet.EphemeralKey, privateKey);
            var mu = GenerateSphinxKey("mu", sharedSecret);
            var payloadToSign = associatedData != null ? packet.PayloadData.Concat(associatedData).ToArray() : packet.PayloadData;
            var computedHmac = HashGenerator.HmacSha256(mu.ToArray(), payloadToSign);

            if (computedHmac == packet.Hmac)
            {
                var rho = GenerateSphinxKey("rho", sharedSecret);
                var cipherStream = GenerateStream(rho.ToArray(), 2 * packet.PayloadData.Length);
                // todo: better variable name here
                var paddedPayload = packet.PayloadData.Concat(Enumerable.Range(0, packet.PayloadData.Length).Select<int, byte>(x => 0x00)).ToArray();
                var binData = ExclusiveOR(paddedPayload, cipherStream).ToArray();

                var sequence = new ReadOnlySequence<byte>(new ReadOnlyMemory<byte>(binData));
                var binReader = new SequenceReader<byte>(sequence);

                // todo: peek payload length
                if (binReader.TryPeek(out var payloadLength))
                {
                    int perHopPayloadLength = 0;

                    if (payloadLength == 0x00)
                    {
                        // todo: this might be deprecated? do we need to support legacy payloads in Lyn?
                        perHopPayloadLength = 65;
                    }
                    else
                    {
                        // safe to truncate because a packet will never be larger than 64KB
                        perHopPayloadLength = (int)binReader.ReadVarInt();
                    }

                    // todo: extract payload bytes from xor'd byte stream using payload length and hmac
                    var perHopPayload = binReader.ReadBytes(perHopPayloadLength);
                    var hopHMAC = binReader.ReadBytes(MAC_LENGTH);

                    // truncated'd again but its safe?
                    var nextOnionPayload = binReader.ReadBytes((int)binReader.Remaining);
                    var nextPublicKey = BlindKey(packet.EphemeralKey, ComputeBlindingFactor(packet.EphemeralKey, sharedSecret));

                    return new DecryptedOnionPacket()
                    {
                        Payload = perHopPayload.ToArray(),
                        NextPacket = new OnionRoutingPacket()
                        {
                            Version = 0x01,
                            EphemeralKey = nextPublicKey,
                            PayloadData = nextOnionPayload.ToArray(),
                            Hmac = hopHMAC.ToArray()
                        },
                        SharedSecret = sharedSecret.ToArray(),
                    };
                }
            }
            else
            {
                throw new Exception("bad hmac");
            }

            throw new Exception("Bah! Humbug!");
        }

    }
}
