namespace Lyn.Protocol.Common.Messages
{
    public class SuccessWithOutputResponse : MessageProcessingOutput
    {
        public SuccessWithOutputResponse(params BoltMessage[] messages)
        {
            Success = true;
            CloseChannel = false;
            ResponseMessages = messages;
        }
    }
}