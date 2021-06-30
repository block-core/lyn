using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3.Types
{
    public class Htlc
    {
        /* What's the status. */

        public HtlcState State;

        /* The unique ID for this peer and this direction (LOCAL or REMOTE) */
        public ulong Id;
        /* The amount in millisatoshi. */

        public MiliSatoshis AmountMsat;

        /* When the HTLC can no longer be redeemed. */

        public uint Expirylocktime;

        /* The hash of the preimage which can redeem this HTLC */

        public UInt256 Rhash;

        /* The preimage which hashes to rhash (if known) */

        public Preimage R;

        /* If they fail the HTLC, we store why here. */

        // failed_htlc *failed;

        /* Routing information sent with this HTLC (outgoing only). */
        public ushort Routing;

        /* Blinding (optional). */

        public PublicKey Blinding;

        public ChannelSide Side
        {
            get
            {
                return State > HtlcState.RcvdAddHtlc ? ChannelSide.Local : ChannelSide.Remote;
            }
        }
    };
}