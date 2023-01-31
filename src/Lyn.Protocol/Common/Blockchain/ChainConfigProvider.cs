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
                    DustLimit = 354,
                    HtlcMinimum = 3500,
                    MaxAcceptedHtlcs = 100,
                    ToSelfDelay = 2016,
                    MaxHtlcValueInFlight = 12000000
                },
                ChannelBoundariesConfig = new ChannelBoundariesConfig
                {
                    MinimumDepth = 6,
                    AllowPrivateChannels = true,
                    ChannelReservePercentage = 0.01m,
                    LargeChannelAmount = 16000000,
                    MaxToSelfDelay = 12500,
                    MinEffectiveHtlcCapacity = 30,
                    TooLargeFeeratePerKw = 12500,
                    TooLowFeeratePerKw = 10
                }
            };
        }
    }
}