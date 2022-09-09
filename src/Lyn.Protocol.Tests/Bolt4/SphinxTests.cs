using Lyn.Protocol.Bolt4;
using Lyn.Protocol.Common.Crypto;
using System;
using System.Linq;
using Xunit;

using Lyn.Protocol.Tests.Bolt4.Data;

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

            var keyList = ephemeralKeys.ToList();
            var secretList = sharedSecrets.ToList();

            Assert.Equal(Convert.FromHexString("02eec7245d6b7d2ccb30380bfbe2a3648cd7a942653f5aa340edcea1f283686619"), keyList.ElementAt(0).GetSpan().ToArray());
            Assert.Equal(Convert.FromHexString("53eb63ea8a3fec3b3cd433b85cd62a4b145e1dda09391b348c4e1cd36a03ea66"), secretList.ElementAt(0));
            Assert.Equal(Convert.FromHexString("028f9438bfbf7feac2e108d677e3a82da596be706cc1cf342b75c7b7e22bf4e6e2"), keyList.ElementAt(1).GetSpan().ToArray());
            Assert.Equal(Convert.FromHexString("a6519e98832a0b179f62123b3567c106db99ee37bef036e783263602f3488fae"), secretList.ElementAt(1));
            Assert.Equal(Convert.FromHexString("03bfd8225241ea71cd0843db7709f4c222f62ff2d4516fd38b39914ab6b83e0da0"), keyList.ElementAt(2).GetSpan().ToArray());
            Assert.Equal(Convert.FromHexString("3a6b412548762f0dbccce5c7ae7bb8147d1caf9b5471c34120b30bc9c04891cc"), secretList.ElementAt(2));
            Assert.Equal(Convert.FromHexString("031dde6926381289671300239ea8e57ffaf9bebd05b9a5b95beaf07af05cd43595"), keyList.ElementAt(3).GetSpan().ToArray());
            Assert.Equal(Convert.FromHexString("21e13c2d7cfe7e18836df50872466117a295783ab8aab0e7ecc8c725503ad02d"), secretList.ElementAt(3));
            Assert.Equal(Convert.FromHexString("03a214ebd875aab6ddfd77f22c5e7311d7f77f17a169e599f157bbcdae8bf071f4"), keyList.ElementAt(4).GetSpan().ToArray());
            Assert.Equal(Convert.FromHexString("b5756b9b542727dbafc6765a49488b023a725d631af688fc031217e90770c328"), secretList.ElementAt(4));
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

            var encryptedOnion = sphinx.CreateOnion(SphinxReferenceVectors.SessionKey, 
                                                    1300, 
                                                    SphinxReferenceVectors.PublicKeys, 
                                                    SphinxReferenceVectors.FixedSizePaymentPayloads, 
                                                    SphinxReferenceVectors.AssociatedData);
            
            var sharedSecrets = encryptedOnion.SharedSecrets.ToArray();

            var decrypted0 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[0], SphinxReferenceVectors.AssociatedData, encryptedOnion.Packet);
            Assert.Equal(SphinxReferenceVectors.FixedSizePaymentPayloads[0], decrypted0.Payload);
            var (secret0, _) = sharedSecrets[0];
            Assert.Equal(secret0, decrypted0.SharedSecret);

            var decrypted1 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[1], SphinxReferenceVectors.AssociatedData, decrypted0.NextPacket);
            var (secret1, _) = sharedSecrets[1];
            Assert.Equal(SphinxReferenceVectors.FixedSizePaymentPayloads[1], decrypted1.Payload);
            Assert.Equal(secret1, decrypted1.SharedSecret);

            var decrypted2 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[2], SphinxReferenceVectors.AssociatedData, decrypted1.NextPacket);
            var (secret2, _) = sharedSecrets[2];
            Assert.Equal(SphinxReferenceVectors.FixedSizePaymentPayloads[2], decrypted2.Payload);
            Assert.Equal(secret2, decrypted2.SharedSecret);

            var decrypted3 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[3], SphinxReferenceVectors.AssociatedData, decrypted2.NextPacket);
            var (secret3, _) = sharedSecrets[3];
            Assert.Equal(SphinxReferenceVectors.FixedSizePaymentPayloads[3], decrypted3.Payload);
            Assert.Equal(secret3, decrypted3.SharedSecret);

            var decrypted4 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[4], SphinxReferenceVectors.AssociatedData, decrypted3.NextPacket);
            var (secret4, _) = sharedSecrets[4];
            Assert.Equal(SphinxReferenceVectors.FixedSizePaymentPayloads[4], decrypted4.Payload);
            Assert.Equal(secret4, decrypted4.SharedSecret);
        }

        [Fact]
        public void CreateOnion_ReferenceTestVector_VariableLengthPayloads()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var encryptedOnion = sphinx.CreateOnion(SphinxReferenceVectors.SessionKey, 1300, SphinxReferenceVectors.PublicKeys, SphinxReferenceVectors.VariableSizePaymentPayloads, SphinxReferenceVectors.AssociatedData);
            var sharedSecrets = encryptedOnion.SharedSecrets.ToArray();

            var decrypted0 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[0], SphinxReferenceVectors.AssociatedData, encryptedOnion.Packet);
            var (secret0, _) = sharedSecrets[0];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloads[0], decrypted0.Payload);
            Assert.Equal(secret0, decrypted0.SharedSecret);

            var decrypted1 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[1], SphinxReferenceVectors.AssociatedData, decrypted0.NextPacket);
            var (secret1, _) = sharedSecrets[1];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloads[1], decrypted1.Payload);
            Assert.Equal(secret1, decrypted1.SharedSecret);

            var decrypted2 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[2], SphinxReferenceVectors.AssociatedData, decrypted1.NextPacket);
            var (secret2, _) = sharedSecrets[2];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloads[2], decrypted2.Payload);
            Assert.Equal(secret2, decrypted2.SharedSecret);

            var decrypted3 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[3], SphinxReferenceVectors.AssociatedData, decrypted2.NextPacket);
            var (secret3, _) = sharedSecrets[3];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloads[3], decrypted3.Payload);
            Assert.Equal(secret3, decrypted3.SharedSecret);

            var decrypted4 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[4], SphinxReferenceVectors.AssociatedData, decrypted3.NextPacket);
            var (secret4, _) = sharedSecrets[4];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloads[4], decrypted4.Payload);
            Assert.Equal(secret4, decrypted4.SharedSecret);
        }

        [Fact]
        public void CreateOnion_ReferenceTestVector_VariableLengthFullPayloads()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var encryptedOnion = sphinx.CreateOnion(SphinxReferenceVectors.SessionKey, 1300, SphinxReferenceVectors.PublicKeys, SphinxReferenceVectors.VariableSizePaymentPayloadsFull, SphinxReferenceVectors.AssociatedData);
            var sharedSecrets = encryptedOnion.SharedSecrets.ToArray();

            var decrypted0 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[0], SphinxReferenceVectors.AssociatedData, encryptedOnion.Packet);
            var (secret0, _) = sharedSecrets[0];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloadsFull[0], decrypted0.Payload);
            Assert.Equal(secret0, decrypted0.SharedSecret);

            var decrypted1 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[1], SphinxReferenceVectors.AssociatedData, decrypted0.NextPacket);
            var (secret1, _) = sharedSecrets[1];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloadsFull[1], decrypted1.Payload);
            Assert.Equal(secret1, decrypted1.SharedSecret);

            var decrypted2 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[2], SphinxReferenceVectors.AssociatedData, decrypted1.NextPacket);
            var (secret2, _) = sharedSecrets[2];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloadsFull[2], decrypted2.Payload);
            Assert.Equal(secret2, decrypted2.SharedSecret);

            var decrypted3 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[3], SphinxReferenceVectors.AssociatedData, decrypted2.NextPacket);
            var (secret3, _) = sharedSecrets[3];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloadsFull[3], decrypted3.Payload);
            Assert.Equal(secret3, decrypted3.SharedSecret);

            var decrypted4 = sphinx.PeelOnion(SphinxReferenceVectors.PrivateKeys[4], SphinxReferenceVectors.AssociatedData, decrypted3.NextPacket);
            var (secret4, _) = sharedSecrets[4];
            Assert.Equal(SphinxReferenceVectors.VariableSizePaymentPayloadsFull[4], decrypted4.Payload);
            Assert.Equal(secret4, decrypted4.SharedSecret);
        }

    }
}
