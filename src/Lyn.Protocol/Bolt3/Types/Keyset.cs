using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3.Types
{
    public class Keyset
    {
        public Keyset(PublicKey revocationKey, PublicKey localHtlcKey, PublicKey remoteHtlcKey, PublicKey localDelayedPaymentKey, PublicKey remotePaymentKey)
        {
            RevocationKey = revocationKey;
            LocalHtlcKey = localHtlcKey;
            RemoteHtlcKey = remoteHtlcKey;
            LocalDelayedPaymentKey = localDelayedPaymentKey;
            RemotePaymentKey = remotePaymentKey;
        }

        public PublicKey RevocationKey { get; set; }
        public PublicKey LocalHtlcKey { get; set; }
        public PublicKey RemoteHtlcKey { get; set; }
        public PublicKey LocalDelayedPaymentKey { get; set; }
        public PublicKey RemotePaymentKey { get; set; }
    };
}