using System;
using System.Text;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class WarningMessageService : IBoltMessageService<WarningMessage>
    {
        private readonly ILogger<WarningMessageService> _logger;
        private readonly IPeerRepository _repository;

        public WarningMessageService(ILogger<WarningMessageService> logger, IPeerRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<WarningMessage> message)
        {
            _logger.LogDebug("Received warning message from {NodeId}", message.NodeId);

            var warningText = message.MessagePayload.Data != null 
                ? Encoding.ASCII.GetString(message.MessagePayload.Data) 
                : string.Empty;

            if (message.MessagePayload.ChannelId != UInt256.Zero)
            {
                _logger.LogWarning("Warning on channel {ChannelId}: {WarningText}",
                    Hex.ToString(message.MessagePayload.ChannelId.GetBytes()), warningText);
            }
            else
            {
                _logger.LogWarning("General warning from peer {NodeId}: {WarningText}", 
                    message.NodeId, warningText);
            }
            
            // Store the warning for peer monitoring - warnings don't close channels
            // but accumulated warnings might indicate a problematic peer
            await _repository.AddErrorMessageToPeerAsync(message.NodeId, new PeerCommunicationIssue
            {
                ChannelId = message.MessagePayload.ChannelId,
                MessageText = warningText,
                MessageType = message.Message.Type,
                IsWarning = true
            });
            
            return new EmptySuccessResponse();
        }
    }
}