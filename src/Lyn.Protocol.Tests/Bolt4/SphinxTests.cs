using Lyn.Protocol.Bolt4;
using Lyn.Protocol.Common.Crypto;
using System;
using System.Linq;
using Xunit;

using Lyn.Protocol.Tests.Bolt4.Data;
using Lyn.Protocol.Bolt4.Entities;

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
        public void GenerateFilter_ReferenceTestVector_FixedSizePayloads()
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
        public void GenerateFilter_ReferenceTestVector_VariableSizePayloads()
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

            foreach(var (payload, expected) in SphinxReferenceVectors.PaylodLengths)
            {
                var actual = sphinx.PeekPayloadLength(payload);
                Assert.Equal(expected, actual);
            }
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

    }
}
