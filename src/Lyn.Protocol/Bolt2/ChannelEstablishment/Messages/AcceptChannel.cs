using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class AcceptChannel : MessagePayload
    {
        public override MessageType MessageType => MessageType.AcceptChannel;
        public ChannelId? TemporaryChannelId { get; set; }
        public Satoshis? DustLimitSatoshis { get; set; }
        public MiliSatoshis? MaxHtlcValueInFlightMsat { get; set; }
        public Satoshis? ChannelReserveSatoshis { get; set; }
        public MiliSatoshis? HtlcMinimumMsat { get; set; }
        public uint MinimumDepth { get; set; }
        public ushort ToSelfDelay { get; set; }
        public ushort MaxAcceptedHtlcs { get; set; }
        public PublicKey? FundingPubkey { get; set; }
        public PublicKey? RevocationBasepoint { get; set; }
        public PublicKey? PaymentBasepoint { get; set; }
        public PublicKey? DelayedPaymentBasepoint { get; set; }
        public PublicKey? HtlcBasepoint { get; set; }
        public PublicKey? FirstPerCommitmentPoint { get; set; }

        // todo: dan add accept_channel_tlvs
    }
}