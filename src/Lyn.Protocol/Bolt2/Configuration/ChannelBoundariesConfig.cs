﻿using Lyn.Protocol.Bolt2.ChannelFlags;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Configuration
{
    public class ChannelBoundariesConfig
    {
        /// <summary>
        /// If <see cref="ChannelFlags.OptionSupportLargeChannel"/> is enabled the node will accept channels
        /// with funding amount bigger then LargeChannel.
        /// The default value of LargeChannel is For 2^24
        /// </summary>
        public Satoshis LargeChannelAmount { get; set; } = 16_777_216; // (2^24)

        public Satoshis MinEffectiveHtlcCapacity { get; set; }

        public ushort MaxToSelfDelay { get; set; }

        public decimal ChannelReservePercentage { get; set; }

        public Satoshis TooLowFeeratePerKw { get; set; }

        public Satoshis TooLargeFeeratePerKw { get; set; }

        public uint MinimumDepth { get; set; }

        public bool AllowPrivateChannels { get; set; }
    }
}