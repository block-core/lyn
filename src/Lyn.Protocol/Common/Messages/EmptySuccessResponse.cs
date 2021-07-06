namespace Lyn.Protocol.Common.Messages
{
    public class EmptySuccessResponse : MessageProcessingOutput
    {
        public EmptySuccessResponse()
        {
            Success = true;
        }
    }
}