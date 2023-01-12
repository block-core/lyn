using System.Text;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common.Messages
{
    public class WarningResponse : MessageProcessingOutput
    {
        public WarningResponse(UInt256 channelId, string message)
        {
            Success = false;
            CloseChannel = false;

            ResponseMessages = new[] { new BoltMessage
            {
                Payload = new WarningMessage
                {
                    ChannelId = channelId,
                    Data = Encoding.ASCII.GetBytes(message ?? string.Empty)
                }
            }};
            
        }
    }
}