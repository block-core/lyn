using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages.TlvRecords
{
    public class FeeRange : TlvRecord
    {
        public ulong MinFeeRange { get; set; }
        public ulong MaxFeeRange { get; set; }
    }
}