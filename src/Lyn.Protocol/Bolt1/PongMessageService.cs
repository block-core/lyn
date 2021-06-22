using System.Threading.Tasks;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt.Messages;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class PongMessageService : IBoltMessageService<PongMessage>
    {
        private readonly ILogger<PongMessageService> _logger;
        private readonly IPingPongMessageRepository _messageRepository;

        public PongMessageService(ILogger<PongMessageService> logger, IPingPongMessageRepository messageRepository)
        {
            _logger = logger;
            _messageRepository = messageRepository;
        }

        public async Task ProcessMessageAsync(PeerMessage<PongMessage> request)
        {
            _logger.LogDebug($"Processing pong from with length {request.Message.BytesLen.ToString()}");

            var pingExists = await _messageRepository.PendingPingExistsForIdAsync(request.NodeId,request.Message.Id); 
            
            if(!pingExists)
                return;

            await _messageRepository.MarkPongReplyForPingAsync(request.NodeId, request.Message.Id);
            
            _logger.LogDebug($"Ping pong has completed successfully for id {request.Message.Id}");
        }
    }
}