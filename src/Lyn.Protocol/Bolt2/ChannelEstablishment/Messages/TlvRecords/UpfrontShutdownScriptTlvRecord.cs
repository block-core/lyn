using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords
{
    public class UpfrontShutdownScriptTlvRecord : TlvRecord
    {
        public byte[]? ShutdownScriptpubkey
        {
            get => Payload;
            set => Payload = value;
        }
    }
}