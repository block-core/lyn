using System;
using System.Collections;
using System.Linq;
using Lyn.Protocol.Bolt9;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt9
{
    public class LynImplementedBoltFeaturesTests
    {
        private LynImplementedBoltFeatures _sut;

        public LynImplementedBoltFeaturesTests()
        {
            _sut = new LynImplementedBoltFeatures(new ParseFeatureFlags());
        }

        [Fact]
        public void ReturnsAllFeaturesAsByteArray()
        {
            var featuresArray = _sut.GetSupportedFeatures();

            Assert.Equal(new byte[] {8, 32}, featuresArray);
        }

        [Fact]
        public void GlobalFeaturesReturnsFirst13Bits()
        {
            var bytes = _sut.GetSupportedFeatures()
                .Take(2)
                .ToArray();

            if (bytes.Length > 1)
            {
                bytes[1] &= (byte) (bytes[1] & (~(1 << 6)));
                bytes[1] &= (byte) (bytes[1] & (~(1 << 7)));
            }

            var globalFeatures = _sut.GetSupportedGlobalFeatures();

            Assert.Equal(bytes, globalFeatures);
        }

        [Fact]
        public void ValidateRemoteFeatureAreCompatibleReturnsTrueWhenFeaturesAreTheSame()
        {
            var localFeaturesBytes = _sut.GetSupportedFeatures();
            var localGlobalFeaturesBytes = _sut.GetSupportedGlobalFeatures();
            
            var result = _sut.ValidateRemoteFeatureAreCompatible(localFeaturesBytes, localGlobalFeaturesBytes);
            
            Assert.True(result);
        }
        
        [Fact]
        public void ValidateRemoteFeatureAreCompatibleReturnsFalseWhenRemoteIsShorterAndAnyOfThemMissingRequired()
        {
            var localFeaturesBytes = _sut.GetSupportedFeatures();
            var testRemote = new byte[localFeaturesBytes.Length];

            var arr = new BitArray(localFeaturesBytes);
            var local = new BitArray(localFeaturesBytes);

            for (var i = 0; i < arr.Length; i += 2)
            {
                arr.SetAll(false);
                arr[i] = !arr[i] ^ local[i]; //make it different to the local bits if required and true
                arr.CopyTo(testRemote, 0);

                var result = _sut.ValidateRemoteFeatureAreCompatible(testRemote, new byte[0]);

                Assert.False(result);
            }
        }

        [Fact]
        public void ValidateRemoteFeatureAreCompatibleReturnsFalseWhenRemoteIsLongerAndAnyOfThemMissingRequired()
        {
            var testRemote = new byte[byte.MaxValue];

            var arr = new BitArray(testRemote);
            var local = new BitArray(_sut.GetSupportedFeatures());


            for (var i = 0; i < arr.Length; i += 2)
            {
                if (i < local.Length)
                {
                    arr[i] = local[i];
                    continue;
                }

                arr[i] = !arr[i]; //set bit to true for the test
                arr.CopyTo(testRemote, 0);

                var result = _sut.ValidateRemoteFeatureAreCompatible(testRemote, new byte[0]);

                Assert.False(result);

                arr[i] = !arr[i]; //clear bit for next iteration
            }
        }
    }
}