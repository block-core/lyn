using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Common
{
    public interface INodeSettings
    {
        PublicKey GetNodeId();
        PrivateKey GetNodePrivateKey();
    }

    class NodeSettings : INodeSettings
    {
        public PublicKey GetNodeId()
        {
            throw new System.NotImplementedException();
        }

        public PrivateKey GetNodePrivateKey()
        {
            throw new System.NotImplementedException();
        }
    }
}