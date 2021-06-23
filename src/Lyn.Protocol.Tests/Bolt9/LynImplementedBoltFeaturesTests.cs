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
            
            Assert.Equal(new byte[] {72},featuresArray);
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
            
            Assert.Equal(bytes,globalFeatures);
        }
    }
}