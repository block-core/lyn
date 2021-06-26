using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common.Blockchain
{
    public class ChainConfigProvider : IChainConfigProvider
    {
        public ChainParameters? GetConfiguration(UInt256 chainHash)
        {
            return new ChainParameters();
        }
    }
}