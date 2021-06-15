using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Messags
{
    public class UpdateAddHtlc : NetworkMessageBase
    {
        private const string COMMAND = "128";

        public override string Command => COMMAND;

        public ChannelId? ChannelId { get; set; }
        public ulong? Id { get; set; }
        public MiliSatoshis? AmountMsat { get; set; }

        public UInt256? PaymentHash { get; set; }

        public uint? CltvExpiry { get; set; }

        public byte[]? OnionRoutingPacket { get; set; } // toto: dan make this a type restricted to 1366*byte
    }
}