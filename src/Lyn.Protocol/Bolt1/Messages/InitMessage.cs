using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt1.Messages
{
    public sealed class InitMessage : MessagePayload
    {
        public override MessageType MessageType => MessageType.Init;

        public byte[] GlobalFeatures { get; set; } = new byte[0];
        public byte[] Features { get; set; } = new byte[0];
    }
}