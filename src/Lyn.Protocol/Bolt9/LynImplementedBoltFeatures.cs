using System.Collections;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt9
{
    public class LynImplementedBoltFeatures : IBoltFeatures
    {
        private const Features FEATURES = Features.InitialRoutingSync | Features.GossipQueries;

        private readonly byte[] _bytes;
        private readonly byte[] _globalBytes;
        public Features SupportedFeatures => FEATURES;

        public LynImplementedBoltFeatures(IParseFeatureFlags parseFeatureFlags)
        {
            _bytes = parseFeatureFlags.ParseFeatures(FEATURES);
            _globalBytes = parseFeatureFlags.ParseNFeatures(FEATURES, 13);
        }
        
        public byte[] GetSupportedFeatures() => _bytes;
        public byte[] GetSupportedGlobalFeatures() => _globalBytes;

        public bool ValidateRemoteFeatureAreCompatible(byte[] remoteNodeFeatures)
        {
            var remoteBitArray = new BitArray(remoteNodeFeatures);
            var localBitArray = new BitArray(_bytes);
            
            return localBitArray.Length > remoteBitArray.Length
                ? CheckFlagsInBothAndLongerForRequiredFields(remoteBitArray, localBitArray)
                : CheckFlagsInBothAndLongerForRequiredFields(localBitArray, remoteBitArray);
        }

        private static bool CheckFlagsInBothAndLongerForRequiredFields(BitArray shortArray, BitArray longArray)
        {
            for (var i = 0; i < longArray.Length; i++)
            {
                if (i < shortArray.Length)
                {
                    if (shortArray[i] != longArray[i] && i % 2 == 0) //If any has a required flag not supported by other node
                        return false;
                }
                else
                {
                    if (longArray[i] && i % 2 == 0) //If the longer array has a required flag not known to the other node
                        return false;
                }
            }

            return true;
        }
    }
}