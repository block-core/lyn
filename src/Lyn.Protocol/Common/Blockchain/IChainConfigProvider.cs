using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common.Blockchain
{
    public interface IChainConfigProvider
    {
        ChainParameters? GetConfiguration(UInt256 chainHash);
    }
}