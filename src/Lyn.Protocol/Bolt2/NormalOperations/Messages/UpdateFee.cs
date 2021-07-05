using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFee : BoltMessage
    {
        private const string COMMAND = "134";
        public override string Command => COMMAND;
        public ChannelId? ChannelId { get; set; }
        public uint? FeeratePerKw { get; set; }
    }
}