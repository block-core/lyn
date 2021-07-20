using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common.Blockchain
{
    public class ChainConfigProvider : IChainConfigProvider
    {
        public ChainParameters? GetConfiguration(UInt256 chainHash)
        {
            return new ChainParameters //dummy data for the test
            {
                Chainhash = chainHash,
                ChannelConfig = new ChannelConfig
                {
                    ChannelReserve = 0,
                    DustLimit = 100,
                    HtlcMinimum = 50000,
                    MaxAcceptedHtlcs = 486,
                    ToSelfDelay = 2160,
                    MaxHtlcValueInFlight = 500000
                },
                ChannelBoundariesConfig = new ChannelBoundariesConfig
                {
                    MinimumDepth = 6,
                    AllowPrivateChannels = true,
                    ChannelReservePercentage = 0.01m,
                    LargeChannelAmount = 16000000,
                    MaxToSelfDelay = 2500,
                    MinEffectiveHtlcCapacity = 500000,
                    TooLargeFeeratePerKw = 2000,
                    TooLowFeeratePerKw = 100
                }
            };
        }
    }
}