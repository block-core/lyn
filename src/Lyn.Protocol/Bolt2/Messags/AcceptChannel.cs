using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Messags
{
    public class AcceptChannel : BoltMessage
    {
        private const string COMMAND = "33";

        public override string Command => COMMAND;

        public ChannelId? TemporaryChannelId { get; set; }
        public Satoshis? DustLimitSatoshis { get; set; }
        public MiliSatoshis? MaxHtlcValueInFlightMsat { get; set; }
        public Satoshis? ChannelReserveSatoshis { get; set; }
        public MiliSatoshis? HtlcMinimumMsat { get; set; }
        public uint? MinimumDepth { get; set; }
        public ushort? ToSelfDelay { get; set; }
        public ushort? MaxAcceptedHtlcs { get; set; }
        public PublicKey? FundingPubkey { get; set; }
        public PublicKey? RevocationBasepoint { get; set; }
        public PublicKey? PaymentBasepoint { get; set; }
        public PublicKey? DelayedPaymentBasepoint { get; set; }
        public PublicKey? HtlcBasepoint { get; set; }
        public PublicKey? FirstPerCommitmentPoint { get; set; }

        // todo: dan add accept_channel_tlvs
    }
}