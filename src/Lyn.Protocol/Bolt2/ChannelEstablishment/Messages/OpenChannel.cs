using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class OpenChannel : MessagePayload
    {
        public override MessageType MessageType => MessageType.OpenChannel;
        public UInt256 TemporaryChannelId { get; set; }
        public UInt256 ChainHash { get; set; }
        public Satoshis FundingSatoshis { get; set; }
        public MiliSatoshis PushMsat { get; set; }
        public Satoshis DustLimitSatoshis { get; set; }
        public MiliSatoshis MaxHtlcValueInFlightMsat { get; set; }
        public Satoshis ChannelReserveSatoshis { get; set; }
        public MiliSatoshis HtlcMinimumMsat { get; set; }
        public uint FeeratePerKw { get; set; }
        public ushort ToSelfDelay { get; set; }
        public ushort MaxAcceptedHtlcs { get; set; }
        public PublicKey FundingPubkey { get; set; }
        public PublicKey RevocationBasepoint { get; set; }
        public PublicKey PaymentBasepoint { get; set; }
        public PublicKey DelayedPaymentBasepoint { get; set; }
        public PublicKey HtlcBasepoint { get; set; }
        public PublicKey FirstPerCommitmentPoint { get; set; }
        public byte ChannelFlags { get; set; }

        // todo: dan add open_channel_tlvs
        
        
        public Basepoints GetBasePoints() => new ()
        {
            DelayedPayment = DelayedPaymentBasepoint,
            Htlc = HtlcBasepoint,
            Payment = PaymentBasepoint,
            Revocation = RevocationBasepoint
        };
    }
}