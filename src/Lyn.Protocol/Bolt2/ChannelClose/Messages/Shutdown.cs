using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages
{
    public class Shutdown : BoltMessage
    {
        private const string COMMAND = "38";
        public override string Command => COMMAND;
        public ChannelId? ChannelId { get; set; }
        public ushort? Lentgh { get; set; }
        public byte[]? ScriptPubkey { get; set; }
    }
}