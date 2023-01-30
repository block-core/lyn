using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages.TlvRecords
{
    public class FeeRange : TlvRecord
    {
        public override ulong Type { get; set; } = 1;
        public ulong MinFeeRange { get; set; }
        public ulong MaxFeeRange { get; set; }
    }
}