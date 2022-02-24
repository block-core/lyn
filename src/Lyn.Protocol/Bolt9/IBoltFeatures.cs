using Lyn.Protocol.Bolt1.Messages;

namespace Lyn.Protocol.Bolt9
{
    public interface IBoltFeatures
    {
        Features SupportedFeatures { get; }

        bool SupportsFeature(Features feature);
        
        byte[] GetSupportedFeatures();
        byte[] GetSupportedGlobalFeatures();

        bool ValidateRemoteFeatureAreCompatible(byte[] remoteNodeFeatures, byte[] remoteNodeGlobalFeatures);

        bool ContainsUnknownRequiredFeatures(byte[] features);
    }
}