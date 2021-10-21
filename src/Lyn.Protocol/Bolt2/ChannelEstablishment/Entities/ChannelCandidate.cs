using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Entities
{
    /// <summary>
    /// A durable state holder for channel establishment.
    /// </summary>
    public class ChannelCandidate
    {
        public UInt256? ChannelId { get; set; }
        public ChannelSide ChannelOpener { get; set; }
        public OpenChannel? OpenChannel { get; set; }
        public AcceptChannel? AcceptChannel { get; set; }
        public FundingCreated? FundingCreated { get; set; }
        public FundingLocked? FundingLocked { get; set; }
        
        public FundingSigned? FundingSignedLocal { get; set; }
        public FundingSigned? FundingSignedRemote { get; set; }
        public byte[]? OpenChannelUpfrontShutdownScript { get; set; }
        public byte[]? AcceptChannelUpfrontShutdownScript { get; set; }

        // do we need to keep the two params bellow?
        // we only need the signatures as the trx can be
        // recreated form the channel information
        public Transaction? RemoteCommitmentTransaction { get; set; }

        public Transaction? LocalCommitmentTransaction { get; set; }

        public Transaction? FundingTransaction { get; set; }
    }
}