using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Messags
{
    public class UpdateFailMalformedHtlc : BoltMessage
    {
        private const string COMMAND = "135";

        public override string Command => COMMAND;

        public ChannelId? ChannelId { get; set; }
        public ulong? Id { get; set; }

        public UInt256? Sha256OfOnion { get; set; }

        public ushort? FailureCode { get; set; }
    }
}