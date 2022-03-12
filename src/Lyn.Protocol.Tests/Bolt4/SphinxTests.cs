using Lyn.Protocol.Bolt4;
using Lyn.Protocol.Common.Crypto;
using Lyn.Types.Fundamental;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt4
{
    public class SphinxTests
    {


        [Fact]
        public void ReferenceTestVector_GeneratesEphemeralKeysAndSecrets()
        {
            // todo: make this test not suck?
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var sessionKey = new PrivateKey(ByteArray.FromHex("4141414141414141414141414141414141414141414141414141414141414141"));
            var publicKeys = new List<PublicKey>()
            {
                new PublicKey(ByteArray.FromHex("02eec7245d6b7d2ccb30380bfbe2a3648cd7a942653f5aa340edcea1f283686619")),
                new PublicKey(ByteArray.FromHex("0324653eac434488002cc06bbfb7f10fe18991e35f9fe4302dbea6d2353dc0ab1c")),
                new PublicKey(ByteArray.FromHex("027f31ebc5462c1fdce1b737ecff52d37d75dea43ce11c74d25aa297165faa2007")),
                new PublicKey(ByteArray.FromHex("032c0b7cf95324a07d05398b240174dc0c2be444d96b159aa6c7f7b1e668680991")),
                new PublicKey(ByteArray.FromHex("02edabbd16b41c8371b92ef2f04c1185b4f03b6dcd52ba9b78d9d7c89c8f221145"))
            };

            var (ephemeralKeys, sharedSecrets) = sphinx.ComputeEphemeralPublicKeysAndSharedSecrets(sessionKey, publicKeys);

            Assert.NotNull(ephemeralKeys);
            Assert.NotNull(sharedSecrets);

            var keyList = ephemeralKeys.ToList();
            var secretList = sharedSecrets.ToList();

            Assert.Equal(ByteArray.FromHex("02eec7245d6b7d2ccb30380bfbe2a3648cd7a942653f5aa340edcea1f283686619"), keyList.ElementAt(0).GetSpan().ToArray());
            Assert.Equal(ByteArray.FromHex("53eb63ea8a3fec3b3cd433b85cd62a4b145e1dda09391b348c4e1cd36a03ea66"), secretList.ElementAt(0));
            Assert.Equal(ByteArray.FromHex("028f9438bfbf7feac2e108d677e3a82da596be706cc1cf342b75c7b7e22bf4e6e2"), keyList.ElementAt(1).GetSpan().ToArray());
            Assert.Equal(ByteArray.FromHex("a6519e98832a0b179f62123b3567c106db99ee37bef036e783263602f3488fae"), secretList.ElementAt(1));
            Assert.Equal(ByteArray.FromHex("03bfd8225241ea71cd0843db7709f4c222f62ff2d4516fd38b39914ab6b83e0da0"), keyList.ElementAt(2).GetSpan().ToArray());
            Assert.Equal(ByteArray.FromHex("3a6b412548762f0dbccce5c7ae7bb8147d1caf9b5471c34120b30bc9c04891cc"), secretList.ElementAt(2));
            Assert.Equal(ByteArray.FromHex("031dde6926381289671300239ea8e57ffaf9bebd05b9a5b95beaf07af05cd43595"), keyList.ElementAt(3).GetSpan().ToArray());
            Assert.Equal(ByteArray.FromHex("21e13c2d7cfe7e18836df50872466117a295783ab8aab0e7ecc8c725503ad02d"), secretList.ElementAt(3));
            Assert.Equal(ByteArray.FromHex("03a214ebd875aab6ddfd77f22c5e7311d7f77f17a169e599f157bbcdae8bf071f4"), keyList.ElementAt(4).GetSpan().ToArray());
            Assert.Equal(ByteArray.FromHex("b5756b9b542727dbafc6765a49488b023a725d631af688fc031217e90770c328"), secretList.ElementAt(4));
        }

        [Fact]
        public void GenerateFilter_ReferenceTestVector_FixedSizePayloads()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var sessionKey = new PrivateKey(ByteArray.FromHex("4141414141414141414141414141414141414141414141414141414141414141"));
            var publicKeys = new List<PublicKey>()
            {
                new PublicKey(ByteArray.FromHex("02eec7245d6b7d2ccb30380bfbe2a3648cd7a942653f5aa340edcea1f283686619")),
                new PublicKey(ByteArray.FromHex("0324653eac434488002cc06bbfb7f10fe18991e35f9fe4302dbea6d2353dc0ab1c")),
                new PublicKey(ByteArray.FromHex("027f31ebc5462c1fdce1b737ecff52d37d75dea43ce11c74d25aa297165faa2007")),
                new PublicKey(ByteArray.FromHex("032c0b7cf95324a07d05398b240174dc0c2be444d96b159aa6c7f7b1e668680991")),
                new PublicKey(ByteArray.FromHex("02edabbd16b41c8371b92ef2f04c1185b4f03b6dcd52ba9b78d9d7c89c8f221145"))
            };

            var referenceFixedSizePaymentPayloads = new List<byte[]>() {
                ByteArray.FromHex("000000000000000000000000000000000000000000000000000000000000000000"),
                ByteArray.FromHex("000101010101010101000000000000000100000001000000000000000000000000"),
                ByteArray.FromHex("000202020202020202000000000000000200000002000000000000000000000000"),
                ByteArray.FromHex("000303030303030303000000000000000300000003000000000000000000000000"),
                ByteArray.FromHex("000404040404040404000000000000000400000004000000000000000000000000")
            };

            var (_, sharedSecrets) = sphinx.ComputeEphemeralPublicKeysAndSharedSecrets(sessionKey, publicKeys);
            var filler = sphinx.GenerateFiller("rho", 
                                                1300, 
                                                sharedSecrets.Take(sharedSecrets.Count - 1), 
                                                referenceFixedSizePaymentPayloads.Take(referenceFixedSizePaymentPayloads.Count - 1)).ToArray();

            var expectedFiller = ByteArray.FromHex("c6b008cf6414ed6e4c42c291eb505e9f22f5fe7d0ecdd15a833f4d016ac974d33adc6ea3293e20859e87ebfb937ba406abd025d14af692b12e9c9c2adbe307a679779259676211c071e614fdb386d1ff02db223a5b2fae03df68d321c7b29f7c7240edd3fa1b7cb6903f89dc01abf41b2eb0b49b6b8d73bb0774b58204c0d0e96d3cce45ad75406be0bc009e327b3e712a4bd178609c00b41da2daf8a4b0e1319f07a492ab4efb056f0f599f75e6dc7e0d10ce1cf59088ab6e873de377343880f7a24f0e36731a0b72092f8d5bc8cd346762e93b2bf203d00264e4bc136fc142de8f7b69154deb05854ea88e2d7506222c95ba1aab065c8a851391377d3406a35a9af3ac");
            Assert.Equal(expectedFiller, filler);
        }

        [Fact]
        public void GenerateFilter_ReferenceTestVector_VariableSizePayloads()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var sessionKey = new PrivateKey(ByteArray.FromHex("4141414141414141414141414141414141414141414141414141414141414141"));
            var publicKeys = new List<PublicKey>()
            {
                new PublicKey(ByteArray.FromHex("02eec7245d6b7d2ccb30380bfbe2a3648cd7a942653f5aa340edcea1f283686619")),
                new PublicKey(ByteArray.FromHex("0324653eac434488002cc06bbfb7f10fe18991e35f9fe4302dbea6d2353dc0ab1c")),
                new PublicKey(ByteArray.FromHex("027f31ebc5462c1fdce1b737ecff52d37d75dea43ce11c74d25aa297165faa2007")),
                new PublicKey(ByteArray.FromHex("032c0b7cf95324a07d05398b240174dc0c2be444d96b159aa6c7f7b1e668680991")),
                new PublicKey(ByteArray.FromHex("02edabbd16b41c8371b92ef2f04c1185b4f03b6dcd52ba9b78d9d7c89c8f221145"))
            };

            var referenceVariableSizePaymentPayloads = new List<byte[]>() {
                ByteArray.FromHex("000000000000000000000000000000000000000000000000000000000000000000"),
                ByteArray.FromHex("140101010101010101000000000000000100000001"),
                ByteArray.FromHex("fd0100000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9fa0a1a2a3a4a5a6a7a8a9aaabacadaeafb0b1b2b3b4b5b6b7b8b9babbbcbdbebfc0c1c2c3c4c5c6c7c8c9cacbcccdcecfd0d1d2d3d4d5d6d7d8d9dadbdcdddedfe0e1e2e3e4e5e6e7e8e9eaebecedeeeff0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                ByteArray.FromHex("140303030303030303000000000000000300000003"),
                ByteArray.FromHex("000404040404040404000000000000000400000004000000000000000000000000")
            };

            var (_, sharedSecrets) = sphinx.ComputeEphemeralPublicKeysAndSharedSecrets(sessionKey, publicKeys);
            var filler = sphinx.GenerateFiller("rho",
                                                1300,
                                                sharedSecrets.Take(sharedSecrets.Count - 1),
                                                referenceVariableSizePaymentPayloads.Take(referenceVariableSizePaymentPayloads.Count - 1)).ToArray();

            var expectedFiller = ByteArray.FromHex("b77d99c935d3f32469844f7e09340a91ded147557bdd0456c369f7e449587c0f5666faab58040146db49024db88553729bce12b860391c29c1779f022ae48a9cb314ca35d73fc91addc92632bcf7ba6fd9f38e6fd30fabcedbd5407b6648073c38331ee7ab0332f41f550c180e1601f8c25809ed75b3a1e78635a2ef1b828e92c9658e76e49f995d72cf9781eec0c838901d0bdde3ac21c13b4979ac9e738a1c4d0b9741d58e777ad1aed01263ad1390d36a18a6b92f4f799dcf75edbb43b7515e8d72cb4f827a9af0e7b9338d07b1a24e0305b5535f5b851b1144bad6238b9d9482b5ba6413f1aafac3cdde5067966ed8b78f7c1c5f916a05f874d5f17a2b7d0ae75d66a5f1bb6ff932570dc5a0cf3ce04eb5d26bc55c2057af1f8326e20a7d6f0ae644f09d00fac80de60f20aceee85be41a074d3e1dda017db79d0070b99f54736396f206ee3777abd4c00a4bb95c871750409261e3b01e59a3793a9c20159aae4988c68397a1443be6370fd9614e46108291e615691729faea58537209fa668a172d066d0efff9bc77c2bd34bd77870ad79effd80140990e36731a0b72092f8d5bc8cd346762e93b2bf203d00264e4bc136fc142de8f7b69154deb05854ea88e2d7506222c95ba1aab065c8a");
            Assert.Equal(expectedFiller, filler);
        }

        [Fact]
        public void PeekPerHopPayloadLength()
        {
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var testCases = new[]
            {
                (ByteArray.FromHex("01"), 34),
                (ByteArray.FromHex("08"), 41),
                (ByteArray.FromHex("00"), 65),
                (ByteArray.FromHex("fc"), 285),
                (ByteArray.FromHex("fd00fd"), 288),
                (ByteArray.FromHex("fdffff"), 65570)
            };

            foreach(var (payload, expected) in testCases)
            {
                var actual = sphinx.PeekPayloadLength(payload);
                Assert.Equal(expected, actual);
            }
        }

    }
}
