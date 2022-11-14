using Lyn.Protocol.Bolt4.Entities;
using Lyn.Protocol.Common.Crypto;
using Lyn.Types.Fundamental;
using Lyn.Types.Onion;
using Lyn.Types.Serialization;
using NaCl.Core;
using NBitcoin.Secp256k1;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4
{
    public class Sphinx : ISphinx
    {
        //todo: move somewhere better
        private const int SPHINX_VERSION = 0;
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

        public ReadOnlySpan<byte> GenerateStream(ReadOnlySpan<byte> keyData, int streamLength)
        {
            var cipher = new ChaCha20(new ReadOnlyMemory<byte>(keyData.ToArray()), 0);
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

        public (IList<PublicKey>, IList<byte[]>) ComputeEphemeralPublicKeysAndSharedSecrets(PrivateKey sessionKey,
                                                                                            IEnumerable<PublicKey> publicKeys)
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

        public (IList<PublicKey>, IList<byte[]>) ComputeEphemeralPublicKeysAndSharedSecrets(PrivateKey sessionKey,
                                                                                            IEnumerable<PublicKey> publicKeys,
                                                                                            IList<PublicKey> ephemeralPublicKeys,
                                                                                            IList<byte[]> blindingFactors,
                                                                                            IList<byte[]> sharedSecrets)
        {
            if (!publicKeys.Any())
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

        public int PeekPayloadLength(byte[] payloadData)
        {
            var sequence = new ReadOnlySequence<byte>(new ReadOnlyMemory<byte>(payloadData));
            var binReader = new SequenceReader<byte>(sequence);

            if (binReader.TryPeek(out var firstByte))
            {
                if (firstByte == 0x00)
                {
                    // todo: consider re-throwing in Peel?
                    throw new InvalidOnionVersionException();
                }

                // safe to truncate because a packet will never be larger than 64KB?
                int perHopPayloadLength = (int)binReader.ReadBigSize();
                perHopPayloadLength += (int)binReader.Consumed; //offset the length by the number of bytes consoomed
                perHopPayloadLength += MAC_LENGTH;
                return perHopPayloadLength;
            }
            else
            {
                throw new ArgumentException("payloadData is empty");
            }
        }

        public ReadOnlySpan<byte> GenerateFiller(string keyType,
                                                 int packetPayloadLength,
                                                 IEnumerable<byte[]> sharedSecrets,
                                                 IEnumerable<byte[]> payloads)
        {
            // todo: asserts
            var secretsAndPayloads = sharedSecrets.Zip(payloads);
            var padding = new List<byte>();
            var filler = secretsAndPayloads.Aggregate(padding, (padding, secretAndPayload) =>
            {
                var (secret, perHopPayload) = secretAndPayload;

                // todo: decide how the hmac comes into play...
                var perHopPayloadLength = PeekPayloadLength(perHopPayload);

                if (perHopPayloadLength != perHopPayload.Length + MAC_LENGTH)
                {
                    throw new Exception("invalid length");
                }

                // assert payload length
                var fillerKey = GenerateSphinxKey(Encoding.ASCII.GetBytes(keyType), secret);
                // todo: byte array matching payload length
                padding.AddRange(Enumerable.Range(0, perHopPayloadLength).Select<int, byte>(x => 0x00));
                var stream = GenerateStream(fillerKey, packetPayloadLength + perHopPayloadLength).ToArray().TakeLast(padding.Count).ToArray();
                var filler = ExclusiveOR(padding.ToArray(), stream).ToArray();
                return filler.ToList();
            });

            return filler.ToArray();
        }

        // TODO: return DecryptedOnionPacket?
        public DecryptedOnionPacket PeelOnion(PrivateKey privateKey, byte[]? associatedData, OnionRoutingPacket packet)
        {
            if (packet.Version != 0)
            {
                // todo: this needs to contain the hash of the packet
                throw new InvalidOnionVersionException();
            }

            var sharedSecret = ComputeSharedSecret(packet.EphemeralKey, privateKey);
            var mu = GenerateSphinxKey("mu", sharedSecret);
            var payloadToSign = associatedData != null ? packet.PayloadData.Concat(associatedData).ToArray() : packet.PayloadData;
            var computedHmac = HashGenerator.HmacSha256(mu.ToArray(), payloadToSign);
            Debug.WriteLine($"computedHmac: {Convert.ToHexString(computedHmac)}");
            Debug.WriteLine($"packet.Hmac: {Convert.ToHexString(packet.Hmac)}");
            if (computedHmac.SequenceEqual(packet.Hmac))
            {
                var rho = GenerateSphinxKey("rho", sharedSecret);
                var cipherStream = GenerateStream(rho.ToArray(), 2 * packet.PayloadData.Length);
                // todo: better variable name here
                var paddedPayload = packet.PayloadData.Concat(new byte[packet.PayloadData.Length]).ToArray();
                var binData = ExclusiveOR(paddedPayload, cipherStream).ToArray();

                var perHopPayloadLength = PeekPayloadLength(binData);

                var sequence = new ReadOnlySequence<byte>(new ReadOnlyMemory<byte>(binData));
                var binReader = new SequenceReader<byte>(sequence);

                // todo: extract payload bytes from xor'd byte stream using payload length and hmac
                var perHopPayload = binReader.ReadBytes(perHopPayloadLength - MAC_LENGTH);
                var hopHMAC = binReader.ReadBytes(MAC_LENGTH);

                // truncated'd again but its safe?
                var nextOnionPayload = binReader.ReadBytes((int)packet.PayloadData.Length);
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
            else
            {
                throw new Exception("bad hmac");
            }

            throw new Exception("Bah! Humbug!");
        }

        private OnionRoutingPacket WrapOnion<T>(ReadOnlySpan<byte> payload,
                                             PublicKey ephemeralPublicKey,
                                             ReadOnlySpan<byte> sharedSecret,
                                             T packet,
                                             byte[]? associatedData,
                                             byte[]? filler = null)
        {
            // todo: verify size
            int packetPayloadLength = 0;
            byte[] currentHmac = null;
            byte[] currentPayload = null;
            if (packet is OnionRoutingPacket onionPacket)
            {
                packetPayloadLength = onionPacket.PayloadData.Length;
                currentPayload = onionPacket.PayloadData;
                currentHmac = onionPacket.Hmac;
            }
            else if (packet is byte[] arr)
            {
                packetPayloadLength = arr.Length;
                currentPayload = arr;
                currentHmac = Enumerable.Range(0, MAC_LENGTH).Select<int, byte>(x => 0x00).ToArray();
            }
            else
            {
                throw new Exception("unsupported packet type specified for T");
            }

            // onionPayload1
            var nextOnionPayload = payload.ToArray().Concat(currentHmac).Concat(currentPayload.SkipLast(payload.Length + MAC_LENGTH)).ToArray();
            var secondPayloadStream = GenerateStream(GenerateSphinxKey("rho", sharedSecret), packetPayloadLength);
            nextOnionPayload = ExclusiveOR(nextOnionPayload, secondPayloadStream).ToArray();

            if (filler != null)
            {
                nextOnionPayload = nextOnionPayload.SkipLast(filler.Length).Concat(filler).ToArray();
            }

            var hmacBytes = nextOnionPayload;
            if (associatedData != null)
            {
                hmacBytes = nextOnionPayload.Concat(associatedData).ToArray();
            }

            var nextHmac = HashGenerator.HmacSha256(GenerateSphinxKey("mu", sharedSecret).ToArray(), hmacBytes);
            Debug.WriteLine($"Computed HMAC: {Convert.ToHexString(nextHmac)}");
            var nextPacket = new OnionRoutingPacket()
            {
                Version = SPHINX_VERSION,
                EphemeralKey = ephemeralPublicKey,
                PayloadData = nextOnionPayload,
                Hmac = nextHmac.ToArray()
            };
            return nextPacket;
        }

        private OnionRoutingPacket RecursivelyCreateOnion(IEnumerable<byte[]> payloads,
                                                          IEnumerable<PublicKey> publicKeys,
                                                          IEnumerable<byte[]> sharedSecrets,
                                                          OnionRoutingPacket packet,
                                                          byte[]? associatedData)
        {
            if (!payloads.Any())
            {
                return packet;
            }
            else
            {
                var nextPacket = WrapOnion(payloads.Last(), publicKeys.Last(), sharedSecrets.Last(), packet, associatedData);
                return RecursivelyCreateOnion(payloads.SkipLast(1), publicKeys.SkipLast(1), sharedSecrets.SkipLast(1), nextPacket, associatedData);
            }
        }

        public PacketAndSecrets CreateOnion(PrivateKey sessionKey,
                                            int packetPayloadLength,
                                            IEnumerable<PublicKey> publicKeys,
                                            IEnumerable<byte[]> payloads,
                                            byte[]? associatedData)
        {
            // todo: verify size of inputs to make sure nothing is too long
            var (ephemeralPublicKeys, sharedSecrets) = ComputeEphemeralPublicKeysAndSharedSecrets(sessionKey, publicKeys);
            var filler = GenerateFiller("rho",
                                        packetPayloadLength,
                                        sharedSecrets.SkipLast(1),
                                        payloads.SkipLast(1)).ToArray();

            // generate the last packet of the route
            var startingBytes = GenerateStream(GenerateSphinxKey("pad", sessionKey), packetPayloadLength).ToArray();
            var lastPacket = WrapOnion(payloads.Last(), ephemeralPublicKeys.Last(), sharedSecrets.Last(), startingBytes, associatedData, filler);

            var onionPacket = RecursivelyCreateOnion(payloads.SkipLast(1), ephemeralPublicKeys.SkipLast(1), sharedSecrets.SkipLast(1), lastPacket, associatedData);

            return new PacketAndSecrets()
            {
                Packet = onionPacket,
                SharedSecrets = sharedSecrets.Zip(ephemeralPublicKeys)
            };
        }
    }
}
