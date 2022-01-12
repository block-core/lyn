using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Common
{
    public interface INodeSettings
    {
        PublicKey GetNodeId();
    }

    class NodeSettings : INodeSettings
    {
        public PublicKey GetNodeId()
        {
            throw new System.NotImplementedException();
        }
    }
}