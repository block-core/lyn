using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Messags
{
    public class UpdateFee : NetworkMessageBase
    {
        private const string COMMAND = "134";

        public override string Command => COMMAND;

        public ChannelId? ChannelId { get; set; }
        public uint? FeeratePerKw { get; set; }
    }
}