using System;
using System.Net;
using System.Threading.Tasks;
using Lyn.Protocol.Common;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class PingMessageService : IBoltMessageService<PingMessage>
    {
        private readonly ILogger<PingMessageService> _logger;

        private const int PING_INTERVAL_SECS = 30;
      
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRandomNumberGenerator _numberGenerator;
        
        private DateTime? _lastPingReceivedDateTime; // the service lifetime will be associated with a node so no need to store in repo
        private readonly IPingPongMessageRepository _messageRepository;

        private readonly IBoltMessageSender<PongMessage> _boltMessageSender;
        
        
        public PingMessageService(ILogger<PingMessageService> logger, IDateTimeProvider dateTimeProvider, 
            IRandomNumberGenerator numberGenerator, IPingPongMessageRepository messageRepository, IBoltMessageSender<PongMessage> boltMessageSender)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dateTimeProvider = dateTimeProvider;
            _numberGenerator = numberGenerator;
            _messageRepository = messageRepository;
            _boltMessageSender = boltMessageSender;
        }

        public async Task ProcessMessageAsync(PeerMessage<PingMessage> request)
        {
            var utcNow = _dateTimeProvider.GetUtcNow();
            
            if (_lastPingReceivedDateTime > utcNow.AddSeconds(-PING_INTERVAL_SECS))
                throw new ProtocolViolationException( //TODO David this case requires failing all the channels with the node
                    $"Ping message can only be received every {PING_INTERVAL_SECS} seconds");

            if (request.Message.NumPongBytes > PingMessage.MAX_BYTES_LEN)
                return;

            _lastPingReceivedDateTime = utcNow;

            _logger.LogDebug($"Send pong to with length {request.Message.NumPongBytes}");
            
            await _boltMessageSender.SendMessageAsync(new PeerMessage<PongMessage>
            {
                NodeId = request.NodeId,
                Message = new PongMessage
                {
                    BytesLen = request.Message.NumPongBytes,
                    Ignored = new byte[request.Message.NumPongBytes]
                }
            });
        }

        public async ValueTask<PingMessage> CreateNewMessageAsync(PublicKey nodeId)
        {
            var bytesLength = _numberGenerator.GetUint16() % PingMessage.MAX_BYTES_LEN;
         
            while(await _messageRepository.PendingPingExistsForIdAsync(nodeId,(ushort) bytesLength))
                bytesLength = _numberGenerator.GetUint16() % PingMessage.MAX_BYTES_LEN;
         
            var pingMessage = new PingMessage((ushort)bytesLength);

            await _messageRepository.AddPingMessageAsync(nodeId, _dateTimeProvider.GetUtcNow(),pingMessage);

            _logger.LogDebug($"Ping generated ,pong length {pingMessage.NumPongBytes}");

            return pingMessage;
        }
    }
}