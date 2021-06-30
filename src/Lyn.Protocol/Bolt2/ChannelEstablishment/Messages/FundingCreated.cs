using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class FundingCreated : BoltMessage
    {
        private const string COMMAND = "34";
        public override string Command => COMMAND;
        public ChannelId? TemporaryChannelId { get; set; }
        public UInt256? FundingTxid { get; set; }
        public ushort? FundingOutputIndex { get; set; }
        public CompressedSignature? Signature { get; set; }
    }
}