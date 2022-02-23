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
            _logger.LogDebug($"Received warning message from {message.NodeId}");

            if (message.MessagePayload.ChannelId != UInt256.Zero)
                _logger.Log(LogLevel.Information,
                    $"Warning on channel {Hex.ToString(message.MessagePayload.ChannelId.GetBytes())}");
            
            if (message.MessagePayload.Data != null)
                _logger.Log(LogLevel.Warning,$"{Encoding.ASCII.GetString(message.MessagePayload.Data)}");
            
            await _repository.AddErrorMessageToPeerAsync(message.NodeId, new PeerCommunicationIssue
            {
                ChannelId = message.MessagePayload.ChannelId,
                MessageText = Encoding.ASCII.GetString(message.MessagePayload.Data ?? ReadOnlySpan<byte>.Empty),
                MessageType = message.Message.Type
            });
            
            return new EmptySuccessResponse();
        }
    }
}