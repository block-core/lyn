using Lyn.Protocol.Bolt2.ChannelFlags;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Common.Blockchain
{
    public class ChainParameters
    {
        public UInt256 Chainhash { get; set; }

        public ChannelConfig ChannelConfig { get; set; }

        public ChannelBoundariesConfig ChannelBoundariesConfig { get; set; }
    }
}