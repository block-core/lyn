using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3.Types
{
    public class HtlcTransactionIn
    {
        public Satoshis FeeratePerKw { get; set; }
        public bool OptionAnchorOutputs { get; set; }
        public uint Sequence { get; set; }
        public uint Locktime { get; set; }
        public MiliSatoshis AmountMsat { get; set; }
        public Satoshis HtlcFee { get; set; }
        public OutPoint CommitOutPoint { get; set; }
        public PublicKey RevocationPubkey { get; set; }
        public PublicKey LocalDelayedkey { get; set; }
        public ushort ToSelfDelay { get; set; }

        public uint CltvExpiry { get; set; }
    }
}