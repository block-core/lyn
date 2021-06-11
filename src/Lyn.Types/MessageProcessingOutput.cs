using Lyn.Types.Bolt.Messages;

namespace Lyn.Types
{
    public class MessageProcessingOutput
    {
        public bool Success { get; set; }

        public ErrorMessage? ErrorMessage { get; set; }
        
        public NetworkMessageBase? ResponseMessage { get; set; }
    }
}