using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt1.Entities
{
    public class PeerCommunicationIssue
    {
        public MessageType MessageType { get; set; }

        public UInt256 ChannelId { get; set; }

        public string MessageText { get; set; }
    }
}