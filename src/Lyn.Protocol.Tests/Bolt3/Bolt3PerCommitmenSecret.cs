using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Shachain;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt3
{
    public class Bolt3PercommitmenSecret
    {
        [Fact]
        public void ShachainLongTest()
        {
            ulong iterationCount = 50;
            Shachain shachain = new Shachain();
            Dictionary<ulong, UInt256> expected = new();
            UInt256 seed = RandomMessages.NewRandomUint256();

            ShachainItems shachainItems = new();

            for (ulong i = Shachain.INDEX_ROOT; i > Shachain.INDEX_ROOT - iterationCount; i--)
            {
                expected.Add(i, shachain.GenerateFromSeed(seed, i));
            }

            for (ulong i = Shachain.INDEX_ROOT; i > Shachain.INDEX_ROOT - iterationCount; i--)
            {
                bool inserted = shachain.InsertSecret(shachainItems, expected[i], i);
                Assert.True(inserted);
                for (ulong j = i; j != Shachain.INDEX_ROOT; j++)
                {
                    var oldsecret = shachain.DeriveOldSecret(shachainItems, j);
                    Assert.NotNull(oldsecret);
                    if (expected[j] != oldsecret)
                    {
                    }

                    Assert.Equal(expected[j], oldsecret);
                }
            }
        }

        [Theory]
        [ClassData(typeof(Bolt3PerCommitmentStorageTestVectors))]
        public void ShachainPercommitmentStorageTests(string name, (bool success, string hash)[] items)
        {
            Shachain shachain = new Shachain();
            ShachainItems shachainItems = new();
            ulong startIndex = 281474976710655;

            foreach (var item in items)
            {
                Assert.Equal(item.success, shachain.InsertSecret(shachainItems, new UInt256(Hex.FromString(item.hash)), startIndex--));

                ulong retestIndex = 281474976710655;
                foreach (var innerItem in items.TakeWhile(t => t.hash == item.hash))
                {
                    Assert.Equal(new UInt256(Hex.FromString(innerItem.hash)), shachain.DeriveOldSecret(shachainItems, retestIndex--));
                }
            }
        }
    }
}