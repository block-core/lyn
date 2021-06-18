using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Messags
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