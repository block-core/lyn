using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt9
{
    public interface IParseFeatureFlags
    {
        Features ParseFeatures(byte[] raw);
        byte[] ParseFeatures(Features features);
        byte[] ParseNFeatures(Features features,int n);
    }
}