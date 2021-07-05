using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFailHtlc : BoltMessage
    {
        private const string COMMAND = "131";
        public override string Command => COMMAND;
        public ChannelId? ChannelId { get; set; }
        public ushort? Length { get; set; }
        public byte[]? Reason { get; set; }
    }
}