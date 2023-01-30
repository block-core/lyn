using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords
{
    public class UpfrontShutdownScriptTlvRecord : TlvRecord
    {
        public override ulong Type { get; set; } = 0;

        public byte[]? ShutdownScriptpubkey
        {
            get => Payload;
            set => Payload = value;
        }
    }
}