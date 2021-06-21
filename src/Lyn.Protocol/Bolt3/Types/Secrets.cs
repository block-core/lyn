using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3.Types
{
    public struct Secrets
    {
        public Secret FundingPrivkey { get; set; }
        public Secret RevocationBasepointSecret { get; set; }
        public Secret PaymentBasepointSecret { get; set; }
        public Secret HtlcBasepointSecret { get; set; }
        public Secret DelayedPaymentBasepointSecret { get; set; }
        public UInt256 Shaseed { get; set; }
    };
}