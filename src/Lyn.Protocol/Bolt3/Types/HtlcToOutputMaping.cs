using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt3.Types
{
    public class HtlcToOutputMaping
    {
        public TransactionOutput TransactionOutput { get; set; }
        public ulong CltvExpirey { get; set; }
        public byte[] WitnessHashRedeemScript { get; set; }
        public Htlc? Htlc { get; set; }
    }
}