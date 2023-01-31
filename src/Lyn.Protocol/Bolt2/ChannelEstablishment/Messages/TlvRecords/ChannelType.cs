using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords
{
    public class ChannelType : TlvRecord
    {
        public override ulong Type { get; set; } = 1;
        
        public byte[]? ShutdownScriptpubkey
        {
            get => Payload;
            set => Payload = value;
        }
    }
}