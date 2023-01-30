using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords
{
    public class ShortChannelIdTlvRecord : TlvRecord
    {
        public override ulong Type { get; set; } = 1;
        
        public ShortChannelId Alias { get; set; }
    }
}