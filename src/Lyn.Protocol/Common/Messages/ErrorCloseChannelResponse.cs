using System.Text;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common.Messages
{
    public class ErrorCloseChannelResponse : MessageProcessingOutput
    {
        public ErrorCloseChannelResponse(UInt256 channelId, string message)
        {
            Success = false;
            CloseChannel = true;

            ResponseMessages = new BoltMessage[]
            {
                new BoltMessage
                {
                    Payload = new ErrorMessage
                    {
                        ChannelId = channelId,
                        Data = Encoding.ASCII.GetBytes(message ?? string.Empty)
                    }
                }
            };
        }
    }
}