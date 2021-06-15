using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Messags
{
    public class FundingSigned : NetworkMessageBase
    {
        private const string COMMAND = "35";

        public override string Command => COMMAND;

        public ChannelId? ChannelId { get; set; }

        public CompressedSignature? Signature { get; set; }
    }
}