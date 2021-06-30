using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3.Types
{
    public class Basepoints
    {
        public PublicKey Revocation { get; set; }
        public PublicKey Payment { get; set; }
        public PublicKey Htlc { get; set; }
        public PublicKey DelayedPayment { get; set; }
    };
}