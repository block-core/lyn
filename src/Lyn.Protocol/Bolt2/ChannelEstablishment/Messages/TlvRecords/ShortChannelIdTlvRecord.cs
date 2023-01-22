using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords
{
    public class ShortChannelIdTlvRecord : TlvRecord
    {
        public ShortChannelId Alias { get; set; }
    }
}