using System;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Common;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class PongMessageService : IControlMessageService<PongMessage>
    {
        private readonly ILogger<PongMessageService> _logger;
        private readonly IPingPongMessageRepository _messageRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        
        public PongMessageService(ILogger<PongMessageService> logger, IPingPongMessageRepository messageRepository, 
            IDateTimeProvider dateTimeProvider)
        {
            _logger = logger;
            _messageRepository = messageRepository;
            _dateTimeProvider = dateTimeProvider;
        }

        public async ValueTask<MessageProcessingOutput> ProcessMessageAsync(PongMessage message, CancellationToken cancellation)
        {
            _logger.LogDebug($"Processing pong from with length {message.BytesLen}");

            var trackedPing = await _messageRepository.GetPingMessageAsync(message.BytesLen); 
            
            if(trackedPing == null)
                return new MessageProcessingOutput();

            await _messageRepository.MarkPongReplyForPingAsync(trackedPing.PingMessage.BytesLen);
            
            _logger.LogDebug($"Ping pong has completed successfully after {_dateTimeProvider.GetUtcNow() - trackedPing.Received}");

            return new MessageProcessingOutput {Success = true};
        }

        public ValueTask<PongMessage> CreateNewMessageAsync()
        {
            throw new InvalidOperationException(); //Pong can only be created as a reply to ping messages
        }
    }
}