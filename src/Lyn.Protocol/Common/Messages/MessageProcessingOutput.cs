namespace Lyn.Protocol.Common.Messages
{
    public class MessageProcessingOutput
    {
        public bool Success { get; set; }

        public bool CloseChannel { get; set; }

        public BoltMessage[]? ResponseMessages { get; set; }
    }
}