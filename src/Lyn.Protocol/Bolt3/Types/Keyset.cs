using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3.Types
{
    public struct Keyset
    {
        public PublicKey LocalRevocationKey { get; set; }
        public PublicKey LocalHtlcKey { get; set; }
        public PublicKey RemoteHtlcKey { get; set; }
        public PublicKey LocalDelayedPaymentKey { get; set; }
        public PublicKey LocalPaymentKey { get; set; }
        public PublicKey RemotePaymentKey { get; set; }
    };
}