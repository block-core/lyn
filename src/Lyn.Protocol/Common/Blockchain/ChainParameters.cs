using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Common.Blockchain
{
    public class ChainParameters
    {
        public UInt256 GenesisBlockhash { get; set; }
        public Satoshis DustLimit { get; set; }
        public Satoshis MaxFunding { get; set; }
        public MiliSatoshis MaxPayment { get; set; }
    }
}