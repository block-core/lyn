using Lyn.Protocol.Bolt2.Entities;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt2.Configuration
{
    public interface IChannelConfigProvider
    {
        ChannelConfig? GetConfiguration(UInt256 chainHash);
    }
}