using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Entities
{
    /// <summary>
    /// A durable state holder for channel establishment.
    /// </summary>
    public class ChannelCandidate
    {
        public ChannelId? ChannelId { get; set; }
        public ChannelSide ChannelOpener { get; set; }
        public OpenChannel? OpenChannel { get; set; }
        public AcceptChannel? AcceptChannel { get; set; }
        public FundingCreated? FundingCreated { get; set; }
        public FundingLocked? FundingLocked { get; set; }
        public FundingSigned? FundingSignedLocal { get; set; }
        public FundingSigned? FundingSignedRemote { get; set; }
        public byte[]? RemoteUpfrontShutdownScript { get; set; }
        public byte[]? LocalUpfrontShutdownScript { get; set; }
    }
}