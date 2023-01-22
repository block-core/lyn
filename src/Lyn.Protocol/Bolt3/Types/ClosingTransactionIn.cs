using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3.Types
{
    public class ClosingTransactionIn
    {
        public ChannelSide SideThatOpenedChannel { get; set; }

        public OutPoint FundingCreatedTxout { get; set; }

        public BitcoinSignature LocalSpendingSignature { get; set; }

        public BitcoinSignature RemoteSpendingSignature { get; set; }


        public byte[] LocalScriptPublicKey { get; set; }
        public byte[] RemoteScriptPublicKey { get; set; }

        public long AmountToPayRemote { get; set; }

        public long AmountToPayLocal { get; set; }

        public Satoshis Fee { get; set; }

        public bool ChannelOpenedFromLocalNode => SideThatOpenedChannel == ChannelSide.Local;
    }
}