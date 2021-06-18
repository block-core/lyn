using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Messags
{
    public class OpenChannel : BoltMessage
    {
        private const string COMMAND = "32";

        public override string Command => COMMAND;

        public ChannelId? TemporaryChannelId { get; set; }
        public ChainHash? ChainHash { get; set; }
        public Satoshis? FundingSatoshis { get; set; }
        public MiliSatoshis? PushMsat { get; set; }
        public Satoshis? DustLimitSatoshis { get; set; }
        public MiliSatoshis? MaxHtlcValueInFlightMsat { get; set; }
        public Satoshis? ChannelReserveSatoshis { get; set; }
        public MiliSatoshis? HtlcMinimumMsat { get; set; }
        public uint? FeeratePerKw { get; set; }
        public ushort? ToSelfDelay { get; set; }
        public ushort? MaxAcceptedHtlcs { get; set; }
        public PublicKey? FundingPubkey { get; set; }
        public PublicKey? RevocationBasepoint { get; set; }
        public PublicKey? PaymentBasepoint { get; set; }
        public PublicKey? DelayedPaymentBasepoint { get; set; }
        public PublicKey? HtlcBasepoint { get; set; }
        public PublicKey? FirstPerCommitmentPoint { get; set; }
        public byte? ChannelFlags { get; set; }

        // todo: dan add open_channel_tlvs
    }
}