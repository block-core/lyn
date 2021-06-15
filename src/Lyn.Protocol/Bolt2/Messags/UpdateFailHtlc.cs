using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Messags
{
    public class UpdateFailHtlc : NetworkMessageBase
    {
        private const string COMMAND = "131";

        public override string Command => COMMAND;

        public ChannelId? ChannelId { get; set; }
        public ushort? Length { get; set; }

        public byte[]? Reason { get; set; }
    }
}