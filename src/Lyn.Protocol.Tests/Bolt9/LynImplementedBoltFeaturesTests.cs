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

            // initial_routing_sync(3), option_upfront_shutdown_script(5), gossip_queries(7),
            // var_onion_optin(9), option_static_remotekey(13), payment_secret(15), basic_mpp(17)
            Assert.Equal(new byte[] { 0xA8, 0xA2, 0x02 }, featuresArray);
        }

        [Fact]
        public void GlobalFeaturesReturnsFirst13Bits()
        {
            var globalFeatures = _sut.GetSupportedGlobalFeatures();

            // Bits up to index 13 are kept (3, 5, 7, 9 and 13 remain set), higher bits cleared.
            Assert.Equal(new byte[] { 0xA8, 0x22, 0x00 }, globalFeatures);
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