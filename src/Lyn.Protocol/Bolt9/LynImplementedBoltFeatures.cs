using System.Collections;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt9
{
    public class LynImplementedBoltFeatures : IBoltFeatures
    {
        private const Features FEATURES = Features.InitialRoutingSync ;//| Features.GossipQueries; this will need to be added back when the gossip queries are fully supported

        private readonly byte[] _bytes;
        private readonly BitArray _FeaturesBitArray;
        private readonly byte[] _globalBytes;
        public Features SupportedFeatures => FEATURES;

        public LynImplementedBoltFeatures(IParseFeatureFlags parseFeatureFlags)
        {
            _bytes = parseFeatureFlags.ParseFeatures(FEATURES);
            _FeaturesBitArray = new BitArray(_bytes);
            _globalBytes = parseFeatureFlags.ParseNFeatures(FEATURES, 13);
        }
        
        public byte[] GetSupportedFeatures() => _bytes;
        public byte[] GetSupportedGlobalFeatures() => _globalBytes;

        public bool ValidateRemoteFeatureAreCompatible(byte[] remoteNodeFeatures, byte[] remoteNodeGlobalFeatures)
        {
            var remoteBitArray = new BitArray(remoteNodeFeatures);
            var remoteGlobalBitArray = new BitArray(remoteNodeGlobalFeatures);

            for (var i = 0; i < remoteGlobalBitArray.Length; i++)
            {
                remoteBitArray[i] = remoteBitArray[i] || remoteGlobalBitArray[i];
            }
            
            return _FeaturesBitArray.Length > remoteBitArray.Length
                ? CheckFlagsInBothAndLongerForRequiredFlags(remoteBitArray, _FeaturesBitArray)
                : CheckFlagsInBothAndLongerForRequiredFlags(_FeaturesBitArray, remoteBitArray);
        }

        public bool ContainsUnknownRequiredFeatures(byte[] features)
        {
            var bits = new BitArray(features);

            for (int i = _FeaturesBitArray.Length; i < bits.Length; i++)
            {
                if (bits[i] && i % 2 == 0)
                    return true;
            }

            return false;
        }

        private static bool CheckFlagsInBothAndLongerForRequiredFlags(BitArray shortArray, BitArray longArray)
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