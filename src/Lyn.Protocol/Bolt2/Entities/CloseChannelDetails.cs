using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Entities
{
    /// <summary>
    /// Represents the state of channel closing negotiation.
    /// </summary>
    public enum ClosingNegotiationState
    {
        /// <summary>
        /// Shutdown has been initiated but no closing_signed sent yet.
        /// </summary>
        ShutdownInitiated,
        
        /// <summary>
        /// Negotiating the closing fee.
        /// </summary>
        Negotiating,
        
        /// <summary>
        /// Both parties have agreed on a fee.
        /// </summary>
        Agreed,
        
        /// <summary>
        /// The closing transaction has been broadcast.
        /// </summary>
        Broadcast,
        
        /// <summary>
        /// The closing transaction has been confirmed.
        /// </summary>
        Confirmed
    }

    public class CloseChannelDetails
    {
        public Satoshis? FeeSatoshis { get; set; }
        public ulong MinFeeRange { get; set; }
        public ulong MaxFeeRange { get; set; }
        public byte[]? RemoteScriptPublicKey { get; set; }
        public CompressedSignature? RemoteNodeScriptPubKeySignature { get; set; }
        public byte[]? LocalScriptPublicKey { get; set; }
        
        /// <summary>
        /// The agreed upon fee after negotiation is complete.
        /// </summary>
        public Satoshis? AgreedFee { get; set; }
        
        /// <summary>
        /// Current state of the closing negotiation.
        /// </summary>
        public ClosingNegotiationState State { get; set; } = ClosingNegotiationState.ShutdownInitiated;
        
        /// <summary>
        /// Number of negotiation rounds completed.
        /// </summary>
        public int NegotiationRounds { get; set; }
    }
}