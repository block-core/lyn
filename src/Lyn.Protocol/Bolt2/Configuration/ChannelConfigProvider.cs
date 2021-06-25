using Lyn.Protocol.Bolt2.Entities;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt2.Configuration
{
    public class ChannelConfigProvider : IChannelConfigProvider
    {
        public ChannelConfig GetConfiguration(UInt256 chainHash)
        {
            return new ChannelConfig();
        }
    }
}