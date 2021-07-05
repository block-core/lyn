using Lyn.Protocol.Bolt2.ChannelFlags;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Common.Blockchain
{
    public class ChainParameters
    {
        public UInt256 GenesisBlockhash { get; set; }

        /// <summary>
        /// If <see cref="ChannelFlags.OptionSupportLargeChannel"/> is enabled the node will accept channels
        /// with funding amount bigger then LargeChannel.
        /// The default value of LargeChannel is For 2^24
        /// </summary>
        public Satoshis LargeChannelAmount { get; set; } = 16_777_216; // (2^24)

        public Satoshis MinFundingAmount { get; set; }

        public ushort MaxToSelfDelay { get; set; }

        public Satoshis TooLowFeeratePerKw { get; set; }
        public Satoshis TooLargeFeeratePerKw { get; set; }
    }
}