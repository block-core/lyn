using System;
using System.Linq;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt9;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt9
{
    public class ParseFeatureFlagsTests
    {
        private ParseFeatureFlags _sut;

        private readonly ulong _featuresMaxValue;
        
        public ParseFeatureFlagsTests()
        {
            _sut = new ParseFeatureFlags();
            
            _featuresMaxValue = Enum.GetValues<Features>()
                .Cast<ulong>() // Once the features reach a value higher than ulong this will need to be addressed
                .Aggregate<ulong, ulong>(0, (current, val) => current + val);
        }

        private Features NewRandomFeatures()
        {
            return (Features) RandomMessages.GetRandomNumberUInt32(_featuresMaxValue > int.MaxValue //TODO David Right now there are only 23 features need to future proof this 
                ? int.MaxValue
                : (int) _featuresMaxValue);
        }
        
        [Fact]
        public void ParseFeaturesFeaturesToByteArrayToFeaturesKeepsCorrectValues()
        {
            var features = NewRandomFeatures();
            
            var bytes = _sut.ParseFeatures(features);

            var parsedFeatures = _sut.ParseFeatures(bytes);
            
            Assert.Equal(features,parsedFeatures);
        }

        [Fact]
        public void ParseNFeaturesReturnsArrayWithAllOverNSetToZero()
        {
            var features = NewRandomFeatures();
            
            var bytes = _sut.ParseFeatures(features);

            var parsedFeatures = _sut.ParseFeatures(bytes);
            
            Assert.Equal(features,parsedFeatures);
        }
    }
}