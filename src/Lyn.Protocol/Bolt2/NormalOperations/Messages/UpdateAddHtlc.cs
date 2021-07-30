using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class UpdateAddHtlc : MessagePayload
    {
        public override MessageType MessageType => MessageType.UpdateAddHtlc;
        public UInt256? ChannelId { get; set; }
        public ulong? Id { get; set; }
        public MiliSatoshis? AmountMsat { get; set; }
        public UInt256? PaymentHash { get; set; }
        public uint? CltvExpiry { get; set; }
        public byte[]? OnionRoutingPacket { get; set; } // toto: dan make this a type restricted to 1366*byte
    }
}