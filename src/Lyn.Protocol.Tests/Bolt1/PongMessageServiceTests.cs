using System;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Types.Bolt.Messages;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt1
{
    public class PongMessageServiceTests
    {
        private PongMessageService _sut;

        private readonly Mock<ILogger<PongMessageService>> _logger;
        private readonly Mock<IPingPongMessageRepository> _messageRepository;

        public PongMessageServiceTests()
        {
            _logger = new Mock<ILogger<PongMessageService>>();
            _messageRepository = new Mock<IPingPongMessageRepository>();

            _sut = new PongMessageService(_logger.Object, _messageRepository.Object);
        }

        [Fact]
        public async Task ProcessMessageAsyncReturnsFalseWhenPingNotFound()
        {
            var pong = new PongMessage
            {
                BytesLen = PingMessage.MAX_BYTES_LEN,
                Ignored = RandomMessages.GetRandomByteArray(PingMessage.MAX_BYTES_LEN)
            };

            var response = await _sut.ProcessMessageAsync(pong, CancellationToken.None);
            
            Assert.False(response.Success);
        }
        
        [Fact]
        public async Task ProcessMessageAsyncUpdatesPingInRepoAndReturnsTrue()
        {
            var pong = new PongMessage
            {
                BytesLen = PingMessage.MAX_BYTES_LEN,
                Ignored = RandomMessages.GetRandomByteArray(PingMessage.MAX_BYTES_LEN)
            };

            _messageRepository.Setup(_ => _.PendingPingExistsForIdAsync(pong.Id))
                .Returns(() => new ValueTask<bool>(true));
            
            var response = await _sut.ProcessMessageAsync(pong, CancellationToken.None);
            
            _messageRepository.Verify(_ => _.MarkPongReplyForPingAsync(pong.Id));
            
            Assert.True(response.Success);
        }

        [Fact]
        public async Task CreateNewMessageAsyncThrowsWhenCalled()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() 
                => _sut.CreateNewMessageAsync().AsTask());
        }
    }
}