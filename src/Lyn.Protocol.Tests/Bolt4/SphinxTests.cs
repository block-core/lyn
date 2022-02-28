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

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        [Fact]
        public void ReferenceTestVector_GeneratesEphemeralKeysAndSecrets()
        {
            // todo: make this test not suck?
            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var sessionKey = new PrivateKey(StringToByteArray("4141414141414141414141414141414141414141414141414141414141414141"));
            var publicKeys = new List<PublicKey>()
            {
                new PublicKey(StringToByteArray("02eec7245d6b7d2ccb30380bfbe2a3648cd7a942653f5aa340edcea1f283686619")),
                new PublicKey(StringToByteArray("0324653eac434488002cc06bbfb7f10fe18991e35f9fe4302dbea6d2353dc0ab1c")),
                new PublicKey(StringToByteArray("027f31ebc5462c1fdce1b737ecff52d37d75dea43ce11c74d25aa297165faa2007")),
                new PublicKey(StringToByteArray("032c0b7cf95324a07d05398b240174dc0c2be444d96b159aa6c7f7b1e668680991")),
                new PublicKey(StringToByteArray("02edabbd16b41c8371b92ef2f04c1185b4f03b6dcd52ba9b78d9d7c89c8f221145"))
            };

            var (ephemeralKeys, sharedSecrets) = sphinx.ComputeEphemeralPublicKeysAndSharedSecrets(sessionKey, publicKeys);

            Assert.NotNull(ephemeralKeys);
            Assert.NotNull(sharedSecrets);

            var keyList = ephemeralKeys.ToList();
            var secretList = sharedSecrets.ToList();

            Assert.Equal(StringToByteArray("02eec7245d6b7d2ccb30380bfbe2a3648cd7a942653f5aa340edcea1f283686619"), keyList.ElementAt(0).GetSpan().ToArray());
            Assert.Equal(StringToByteArray("53eb63ea8a3fec3b3cd433b85cd62a4b145e1dda09391b348c4e1cd36a03ea66"), secretList.ElementAt(0));
            Assert.Equal(StringToByteArray("028f9438bfbf7feac2e108d677e3a82da596be706cc1cf342b75c7b7e22bf4e6e2"), keyList.ElementAt(1).GetSpan().ToArray());
            Assert.Equal(StringToByteArray("a6519e98832a0b179f62123b3567c106db99ee37bef036e783263602f3488fae"), secretList.ElementAt(1));
            Assert.Equal(StringToByteArray("03bfd8225241ea71cd0843db7709f4c222f62ff2d4516fd38b39914ab6b83e0da0"), keyList.ElementAt(2).GetSpan().ToArray());
            Assert.Equal(StringToByteArray("3a6b412548762f0dbccce5c7ae7bb8147d1caf9b5471c34120b30bc9c04891cc"), secretList.ElementAt(2));
            Assert.Equal(StringToByteArray("031dde6926381289671300239ea8e57ffaf9bebd05b9a5b95beaf07af05cd43595"), keyList.ElementAt(3).GetSpan().ToArray());
            Assert.Equal(StringToByteArray("21e13c2d7cfe7e18836df50872466117a295783ab8aab0e7ecc8c725503ad02d"), secretList.ElementAt(3));
            Assert.Equal(StringToByteArray("03a214ebd875aab6ddfd77f22c5e7311d7f77f17a169e599f157bbcdae8bf071f4"), keyList.ElementAt(4).GetSpan().ToArray());
            Assert.Equal(StringToByteArray("b5756b9b542727dbafc6765a49488b023a725d631af688fc031217e90770c328"), secretList.ElementAt(4));
        }

    }
}
