using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFee : MessagePayload
    {
        public override MessageType MessageType => MessageType.UpdateFee;
        public ChannelId? ChannelId { get; set; }
        public uint? FeeratePerKw { get; set; }
    }
}