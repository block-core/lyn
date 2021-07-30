using System;
using System.Text;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Common.Messages
{
    public class MessageProcessingOutput
    {
        public static MessageProcessingOutput CreateErrorMessage(UInt256 channelId, bool closeChannel, string? message = null)
        {
            return new MessageProcessingOutput
            {
                CloseChannel = closeChannel,
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
                }
            };
        }

        public bool Success { get; set; }

        public bool CloseChannel { get; set; }

        public BoltMessage[]? ResponseMessages { get; set; }
    }
}