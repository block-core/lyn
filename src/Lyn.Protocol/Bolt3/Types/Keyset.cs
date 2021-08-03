using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3.Types
{
    public class Keyset
    {
        public Keyset(PublicKey localRevocationKey, PublicKey localHtlcKey, PublicKey remoteHtlcKey, PublicKey localDelayedPaymentKey, PublicKey localPaymentKey, PublicKey remotePaymentKey)
        {
            LocalRevocationKey = localRevocationKey;
            LocalHtlcKey = localHtlcKey;
            RemoteHtlcKey = remoteHtlcKey;
            LocalDelayedPaymentKey = localDelayedPaymentKey;
            LocalPaymentKey = localPaymentKey;
            RemotePaymentKey = remotePaymentKey;
        }

        public PublicKey LocalRevocationKey { get; set; }
        public PublicKey LocalHtlcKey { get; set; }
        public PublicKey RemoteHtlcKey { get; set; }
        public PublicKey LocalDelayedPaymentKey { get; set; }
        public PublicKey LocalPaymentKey { get; set; }
        public PublicKey RemotePaymentKey { get; set; }
    };
}