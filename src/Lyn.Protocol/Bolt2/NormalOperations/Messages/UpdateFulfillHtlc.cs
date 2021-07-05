using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateFulfillHtlc : BoltMessage
    {
        private const string COMMAND = "130";
        public override string Command => COMMAND;
        public ChannelId? ChannelId { get; set; }
        public ulong? Id { get; set; }
        public Preimage? PaymentPreimage { get; set; }
    }
}