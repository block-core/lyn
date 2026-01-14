using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class CommitmentSigned : MessagePayload
    {
        public override MessageType MessageType => MessageType.CommitmentSigned;
        public UInt256? ChannelId { get; set; }
        public CompressedSignature? Signature { get; set; }
        public ushort? NumHtlcs { get; set; }
        
        /// <summary>
        /// Array of HTLC signatures, one for each HTLC output.
        /// </summary>
        public CompressedSignature[]? HtlcSignatures { get; set; }
    }
}