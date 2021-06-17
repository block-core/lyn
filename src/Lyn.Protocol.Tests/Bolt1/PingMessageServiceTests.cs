using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Common;
using Lyn.Types.Bolt.Messages;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt1
{
    public class PingMessageServiceTests
    {
        private readonly PingMessageService _sut;

        private readonly Mock<ILogger<PingMessageService>> _logger;
        private readonly Mock<IDateTimeProvider> _dateTimeProvider;
        private readonly Mock<IRandomNumberGenerator> _randomNumberGenerator;
        private readonly Mock<IPingPongMessageRepository> _messageRepository;

        private DateTime _utcNow;

        public PingMessageServiceTests()
        {
            _logger = new Mock<ILogger<PingMessageService>>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();
            _randomNumberGenerator = new Mock<IRandomNumberGenerator>();
            _messageRepository = new Mock<IPingPongMessageRepository>();

            _sut = new PingMessageService(_logger.Object, _dateTimeProvider.Object,
                _randomNumberGenerator.Object, _messageRepository.Object);

            _utcNow = DateTime.UtcNow;

            _dateTimeProvider.Setup(_ => _.GetUtcNow())
                .Returns(() => _utcNow);
        }

        [Fact]
        public async Task ProcessMessageAsyncWhenMessageIsLongerThanAllowedFailsTheMessage()
        {
            var message = new PingMessage(PingMessage.MAX_BYTES_LEN + 1);

            var result = await _sut.ProcessMessageAsync(message, CancellationToken.None);
            
            Assert.False(result.Success);
        }

        
        [Fact]
        public async Task ProcessMessageAsyncReturnsSuccessWithAPongMessage()
        {
            var message = new PingMessage(PingMessage.MAX_BYTES_LEN);

            var result = await _sut.ProcessMessageAsync(message, CancellationToken.None);
 
            Assert.True(result.Success);
            Assert.IsType<PongMessage>(result.ResponseMessage);
            Assert.Equal(message.NumPongBytes,((PongMessage)result.ResponseMessage!).BytesLen);
            Assert.Equal(message.NumPongBytes,((PongMessage)result.ResponseMessage!).Ignored!.Length);
        }
        
        [Fact]
        public async Task ProcessMessageAsyncThrowsIfPingWasReceivedBeforeTheAllowedTime()
        {
            var message = new PingMessage(PingMessage.MAX_BYTES_LEN);

            await _sut.ProcessMessageAsync(message, CancellationToken.None);
 
            await Assert.ThrowsAsync<ProtocolViolationException>(() 
                => _sut.ProcessMessageAsync(message,CancellationToken.None).AsTask());
        }
    }
}