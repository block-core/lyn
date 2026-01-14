using System;
using System.Text;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class ErrorMessageService : IBoltMessageService<ErrorMessage>
    {
        private readonly IPeerRepository _repository;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<ErrorMessageService> _logger;

        public ErrorMessageService(
            ILogger<ErrorMessageService> logger, 
            IPeerRepository repository,
            IConnectionManager connectionManager)
        {
            _logger = logger;
            _repository = repository;
            _connectionManager = connectionManager;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ErrorMessage> request)
        {
            _logger.LogDebug("Received error message from {NodeId}", request.NodeId);

            var errorText = request.MessagePayload.Data != null 
                ? Encoding.ASCII.GetString(request.MessagePayload.Data) 
                : string.Empty;
            
            if (!string.IsNullOrEmpty(errorText))
                _logger.LogDebug("Error message content: {ErrorText}", errorText);

            await _repository.AddErrorMessageToPeerAsync(request.NodeId, new PeerCommunicationIssue
            {
                ChannelId = request.MessagePayload.ChannelId,
                MessageText = errorText,
                MessageType = request.Message.Type
            });

            // BOLT1: If channel_id is all zeros, fail all channels and close connection
            if (request.MessagePayload.ChannelId == UInt256.Zero)
            {
                _logger.LogWarning("Received error with all-zero channel_id from {NodeId}, failing all channels", 
                    request.NodeId);
                await _connectionManager.FailAllChannelsAsync(request.NodeId, errorText);
            }
            else
            {
                _logger.LogWarning("Received error for channel {ChannelId} from {NodeId}, failing channel",
                    request.MessagePayload.ChannelId, request.NodeId);
                await _connectionManager.FailChannelAsync(request.NodeId, request.MessagePayload.ChannelId, errorText);
            }

            return new EmptySuccessResponse();
        }
    }
}