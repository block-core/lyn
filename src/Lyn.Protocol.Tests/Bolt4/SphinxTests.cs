using Lyn.Protocol.Bolt4;
using Lyn.Protocol.Common.Crypto;
using System;
using System.Linq;
using Xunit;

using Lyn.Protocol.Tests.Bolt4.Data;
using Lyn.Protocol.Bolt4.Entities;
using System.Collections.Generic;
using Lyn.Types.Fundamental;
using Lyn.Types.Onion;

namespace Lyn.Protocol.Tests.Bolt4
{
    public class SphinxTests
    {

        [Fact]
        public void ReferenceTestVector_GeneratesEphemeralKeysAndSecrets()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var (ephemeralKeys, sharedSecrets) = sphinx.ComputeEphemeralPublicKeysAndSharedSecrets(SphinxReferenceVectors.SessionKey,
                                                                                                   SphinxReferenceVectors.PublicKeys);

            Assert.NotNull(ephemeralKeys);
            Assert.NotNull(sharedSecrets);
            Assert.Equal(5, sharedSecrets.Count);

            var keyList = ephemeralKeys.ToList();
            var secretList = sharedSecrets.ToList();

            for (var i = 0; i < sharedSecrets.Count; i++)
            {
                Assert.Equal(SphinxReferenceVectors.ExpectedEphemeralKeys[i], keyList.ElementAt(i).GetSpan().ToArray());
                Assert.Equal(SphinxReferenceVectors.ExpectedEphemeralSecrets[i], secretList.ElementAt(i));
            }
        }

        [Fact]
        public void GenerateFiller_ReferenceTestVector_FixedSizePayloads()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var (_, sharedSecrets) = sphinx.ComputeEphemeralPublicKeysAndSharedSecrets(SphinxReferenceVectors.SessionKey,
                                                                                       SphinxReferenceVectors.PublicKeys);
            var filler = sphinx.GenerateFiller("rho",
                                                1300,
                                                sharedSecrets.SkipLast(1),
                                                SphinxReferenceVectors.FixedSizePaymentPayloads.SkipLast(1)).ToArray();

            Assert.Equal(SphinxReferenceVectors.FixedSizePayload_ExpectedFiller, filler);
        }

        [Fact]
        public void GenerateFiller_ReferenceTestVector_VariableSizePayloads()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var (_, sharedSecrets) = sphinx.ComputeEphemeralPublicKeysAndSharedSecrets(SphinxReferenceVectors.SessionKey,
                                                                                       SphinxReferenceVectors.PublicKeys);
            var filler = sphinx.GenerateFiller("rho",
                                                1300,
                                                sharedSecrets.SkipLast(1),
                                                SphinxReferenceVectors.VariableSizePaymentPayloads.SkipLast(1)).ToArray();

            Assert.Equal(SphinxReferenceVectors.VariableSizePayload_ExpectedFiller, filler);
        }

        [Fact]
        public void PeekPerHopPayloadLength()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            foreach (var (payload, expected) in SphinxReferenceVectors.PaylodLengths)
            {
                var actual = sphinx.PeekPayloadLength(payload);
                Assert.Equal(expected, actual);
            }

            Assert.Throws<InvalidOnionVersionException>(() => sphinx.PeekPayloadLength(SphinxReferenceVectors.InvalidVersionPayload));
        }

        [Fact]
        public void CreateOnion_ReferenceTestVector_FixedLengthPayloads()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            PacketAndSecrets encryptedOnion = sphinx.CreateOnion(SphinxReferenceVectors.SessionKey,
                                                    1300,
                                                    SphinxReferenceVectors.PublicKeys,
                                                    SphinxReferenceVectors.FixedSizePaymentPayloads,
                                                    SphinxReferenceVectors.AssociatedData);

            var sharedSecrets = encryptedOnion.SharedSecrets.ToArray();
            Assert.Equal(5, sharedSecrets.Length);

            OnionRoutingPacket currentPacket = encryptedOnion.Packet;
            for (var i = 0; i < sharedSecrets.Length; i++)
            {
                var decrypted = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[i], SphinxReferenceVectors.AssociatedData, currentPacket);
                Assert.Equal(SphinxReferenceVectors.FixedSizePaymentPayloads[i], decrypted.Payload);
                var (secret, _) = sharedSecrets[i];
                Assert.Equal(secret, decrypted.SharedSecret);
                currentPacket = decrypted.NextPacket;
            }
        }

        [Fact]
        public void CreateOnion_ReferenceTestVector_VariableLengthPayloads()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var encryptedOnion = sphinx.CreateOnion(SphinxReferenceVectors.SessionKey, 1300, SphinxReferenceVectors.PublicKeys, SphinxReferenceVectors.VariableSizePaymentPayloads, SphinxReferenceVectors.AssociatedData);
            var sharedSecrets = encryptedOnion.SharedSecrets.ToArray();

            OnionRoutingPacket currentPacket = encryptedOnion.Packet;
            for (var i = 0; i < sharedSecrets.Length; i++)
            {
                var decrypted = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[i], SphinxReferenceVectors.AssociatedData, currentPacket);
                Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloads[i], decrypted.Payload);
                var (secret, _) = sharedSecrets[i];
                Assert.Equal(secret, decrypted.SharedSecret);
                currentPacket = decrypted.NextPacket;
            }
        }

        [Fact]
        public void CreateOnion_ReferenceTestVector_VariableLengthFullPayloads()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var encryptedOnion = sphinx.CreateOnion(SphinxReferenceVectors.SessionKey, 1300, SphinxReferenceVectors.PublicKeys, SphinxReferenceVectors.VariableSizePaymentPayloadsFull, SphinxReferenceVectors.AssociatedData);
            var sharedSecrets = encryptedOnion.SharedSecrets.ToArray();

            OnionRoutingPacket currentPacket = encryptedOnion.Packet;
            for (var i = 0; i < sharedSecrets.Length; i++)
            {
                var decrypted = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[i], SphinxReferenceVectors.AssociatedData, currentPacket);
                Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloadsFull[i], decrypted.Payload);
                var (secret, _) = sharedSecrets[i];
                Assert.Equal(secret, decrypted.SharedSecret);
                currentPacket = decrypted.NextPacket;
            }
        }

        [Fact]
        public void IsLastPacket()
        {
            var publicKeys = SphinxReferenceVectors.PublicKeys;
            var testCases = new[]
            {
                // Bolt 1.0 payloads use the next packet's hmac to signal termination.
                (true, new DecryptedOnionPacket() { Payload = new [] {(byte)0x00},
                                                    NextPacket = new OnionRoutingPacket() { Version = 0,
                                                                                            EphemeralKey = publicKeys[0],
                                                                                            PayloadData = new byte[32],
                                                                                            Hmac = ByteVector32.Zeroes },
                                                    SharedSecret = ByteVector32.One
                                                  }
                ),
                (false, new DecryptedOnionPacket() { Payload = new [] {(byte)0x00},
                                                    NextPacket = new OnionRoutingPacket() { Version = 0,
                                                                                            EphemeralKey = publicKeys[0],
                                                                                            PayloadData = new byte[32],
                                                                                            Hmac = ByteVector32.One },
                                                    SharedSecret = ByteVector32.One
                                                  }
                ),
                // Bolt 1.1 payloads currently also use the next packet's hmac to signal termination.
                (true, new DecryptedOnionPacket() { Payload = Convert.FromHexString("0101"),
                                                    NextPacket = new OnionRoutingPacket() { Version = 0,
                                                                                            EphemeralKey = publicKeys[0],
                                                                                            PayloadData = new byte[32],
                                                                                            Hmac = ByteVector32.Zeroes },
                                                    SharedSecret = ByteVector32.One
                                                  }
                ),
                (false, new DecryptedOnionPacket() { Payload = Convert.FromHexString("0101"),
                                                    NextPacket = new OnionRoutingPacket() { Version = 0,
                                                                                            EphemeralKey = publicKeys[0],
                                                                                            PayloadData = new byte[32],
                                                                                            Hmac = ByteVector32.One },
                                                    SharedSecret = ByteVector32.One
                                                  }
                ),
                (false, new DecryptedOnionPacket() { Payload = Convert.FromHexString("0100"),
                                                    NextPacket = new OnionRoutingPacket() { Version = 0,
                                                                                            EphemeralKey = publicKeys[0],
                                                                                            PayloadData = new byte[32],
                                                                                            Hmac = ByteVector32.One },
                                                    SharedSecret = ByteVector32.One
                                                  }
                )
            };

            foreach (var (expected, packet) in testCases)
            {
                Assert.Equal(expected, packet.IsLastPacket);
            }
        }

        [Fact]
        public void Peel_BadOnion_BadVersion_Throws()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var packet = new OnionRoutingPacket()
            {
                Version = 1,
                EphemeralKey = SphinxReferenceVectors.PublicKeys[0],
                PayloadData = ByteVector.Fill(65, 0x01),
                Hmac = Convert.FromHexString("C908EE9582217D3B58D75FAC05CD5DBBEF91C1841A5B59D521283F9F4C43B643")
            };

            Assert.Throws<InvalidOnionVersionException>(() => sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[0], SphinxReferenceVectors.AssociatedData, packet));
        }

    }

    public static class ByteVector32
    {
        public static byte[] Zeroes = Enumerable.Repeat((byte)0, 32).ToArray();
        public static byte[] One = Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000000001");

    }

    public static class ByteVector
    {
        public static byte[] Fill(int length, byte value)
        {
            return Enumerable.Repeat(value, length).ToArray();
        }
    }


}
