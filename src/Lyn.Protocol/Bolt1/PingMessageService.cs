using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Common;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class PingMessageService : IControlMessageService<PingMessage>
    {
        private readonly ILogger<PingMessageService> _logger;

        private const int PING_INTERVAL_SECS = 30;
      
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRandomNumberGenerator _numberGenerator;
        
        private DateTime? _lastPingReceivedDateTime; // the service lifetime will be associated with a node so no need to store in repo
        private readonly IPingPongMessageRepository _messageRepository;
        
        
        public PingMessageService(ILogger<PingMessageService> logger, IDateTimeProvider dateTimeProvider, 
            IRandomNumberGenerator numberGenerator, IPingPongMessageRepository messageRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dateTimeProvider = dateTimeProvider;
            _numberGenerator = numberGenerator;
            _messageRepository = messageRepository;
        }

        public async ValueTask<MessageProcessingOutput> ProcessMessageAsync(PingMessage message, CancellationToken cancellation)
        {
            if (_lastPingReceivedDateTime > _dateTimeProvider.GetUtcNow().AddSeconds(-PING_INTERVAL_SECS))
                throw new ProtocolViolationException( //TODO David examine returning success false *this edge case requires failing the channel
                    $"Ping message can only be received every {PING_INTERVAL_SECS} seconds");

            if (message.NumPongBytes > PingMessage.MAX_BYTES_LEN)
                return new MessageProcessingOutput();

            _lastPingReceivedDateTime = _dateTimeProvider.GetUtcNow();

            _logger.LogDebug($"Send pong to with length {message.NumPongBytes}");
         
            // will prevent to handle noise messages to other Processors
            return new MessageProcessingOutput
            {
                Success = true,
                ResponseMessage = new PongMessage
                {
                    BytesLen = message.NumPongBytes,
                    Ignored = new byte[message.NumPongBytes]
                }
            };
        }

        public async ValueTask<PingMessage> CreateNewMessageAsync()
        {
            var bytesLength = _numberGenerator.GetUint16() % PingMessage.MAX_BYTES_LEN;
         
            while(await _messageRepository.PendingPingExistsForIdAsync((ushort) bytesLength))
                bytesLength = _numberGenerator.GetUint16() % PingMessage.MAX_BYTES_LEN;
         
            var pingMessage = new PingMessage((ushort)bytesLength);

            await _messageRepository.AddPingMessageAsync(_dateTimeProvider.GetUtcNow(),pingMessage);

            _logger.LogDebug($"Ping generated ,pong length {pingMessage.NumPongBytes}");

            return pingMessage;
        }
    }
}