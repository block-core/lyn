using Lyn.Protocol.Bolt1.Messages;

namespace Lyn.Protocol.Common
{
    public class MessageProcessingOutput
    {
        public bool Success { get; set; }

        public bool CloseChannel { get; set; }

        public BoltMessage[]? ResponseMessages { get; set; }
    }
}