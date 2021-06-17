using System;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class PongMessageService : IControlMessageService<PongMessage>
    {
        private readonly ILogger<PongMessageService> _logger;
        private readonly IPingPongMessageRepository _messageRepository;

        public PongMessageService(ILogger<PongMessageService> logger, IPingPongMessageRepository messageRepository)
        {
            _logger = logger;
            _messageRepository = messageRepository;
        }

        public async ValueTask<MessageProcessingOutput> ProcessMessageAsync(PongMessage message, CancellationToken cancellation)
        {
            _logger.LogDebug($"Processing pong from with length {message.BytesLen}");

            var pingExists = await _messageRepository.PendingPingWithIdExistsAsync(message.Id); 
            
            if(!pingExists)
                return new MessageProcessingOutput();

            await _messageRepository.MarkPongReplyForPingAsync(message.Id);
            
            _logger.LogDebug($"Ping pong has completed successfully for id {message.Id}");

            return new MessageProcessingOutput {Success = true};
        }

        public ValueTask<PongMessage> CreateNewMessageAsync()
        {
            throw new InvalidOperationException(); //Pong can only be created as a reply to ping messages
        }
    }
}