using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Messags
{
    public class CommitmentSigned : BoltMessage
    {
        private const string COMMAND = "132";

        public override string Command => COMMAND;

        public ChannelId? ChannelId { get; set; }
        public ushort? NumHtlcs { get; set; }

        public CompressedSignature? Signature { get; set; }
        public byte? HtlcSignature { get; set; }
    }
}