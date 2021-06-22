using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Entities
{
    public class ChannelConfig
    {
        /* BOLT #2:
         *
         * `dust_limit_satoshis` is the threshold below which outputs should
         * not be generated for this node's commitment or HTLC transaction */
        public Satoshis DustLimit { get; set; }

        /* BOLT #2:
         *
         * `max_htlc_value_in_flight_msat` is a cap on total value of
         * outstanding HTLCs, which allows a node to limit its exposure to
         * HTLCs */
        public MiliSatoshis MaxHtlcValueInFlight { get; set; }

        /* BOLT #2:
         *
         * `channel_reserve_satoshis` is the minimum amount that the other
         * node is to keep as a direct payment. */
        public Satoshis ChannelReserve { get; set; }

        /* BOLT #2:
         *
         * `htlc_minimum_msat` indicates the smallest value HTLC this node
         * will accept.
         */
        public MiliSatoshis HtlcMinimum { get; set; }

        /* BOLT #2:
         *
         * `to_self_delay` is the number of blocks that the other node's
         * to-self outputs must be delayed, using `OP_CHECKSEQUENCEVERIFY`
         * delays */
        private ushort ToSelfDelay { get; set; }

        /* BOLT #2:
         *
         * similarly, `max_accepted_htlcs` limits the number of outstanding
         * HTLCs the other node can offer. */
        private ushort MaxAcceptedHtlcs { get; set; }
    }
}