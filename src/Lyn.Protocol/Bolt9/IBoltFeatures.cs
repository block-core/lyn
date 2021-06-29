using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt9
{
    public interface IBoltFeatures
    {
        Features SupportedFeatures { get; }

        byte[] GetSupportedFeatures();
        byte[] GetSupportedGlobalFeatures();

        bool ValidateRemoteFeatureAreCompatible(byte[] remoteNodeFeatures, byte[] remoteNodeGlobalFeatures);

        bool ContainsUnknownRequiredFeatures(byte[] features);
    }
}