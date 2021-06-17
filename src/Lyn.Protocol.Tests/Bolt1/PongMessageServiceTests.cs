using System.Security.Cryptography;
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
    public class PongMessageServiceTests
    {
        private PongMessageService _sut;

        private readonly Mock<ILogger<PongMessageService>> _logger;
        private readonly Mock<IPingPongMessageRepository> _messageRepository;
        private readonly Mock<IDateTimeProvider> _dateTimeProvider;
        
        public PongMessageServiceTests()
        {
            _logger = new Mock<ILogger<PongMessageService>>();
            _messageRepository = new Mock<IPingPongMessageRepository>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();

            _sut = new PongMessageService(_logger.Object, _messageRepository.Object, _dateTimeProvider.Object);
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
            _logger.VerifyAll();
        }
        
        [Fact]
        public async Task ProcessMessageAsyncUpdatesPingInRepoAndReturnsTrue()
        {
            var pong = new PongMessage
            {
                BytesLen = PingMessage.MAX_BYTES_LEN,
                Ignored = RandomMessages.GetRandomByteArray(PingMessage.MAX_BYTES_LEN)
            };

            _messageRepository.Setup(_ => _.PendingPingWithIdExistsAsync(pong.BytesLen))
                .Returns(() => new ValueTask<bool>(true));
            
            var response = await _sut.ProcessMessageAsync(pong, CancellationToken.None);
            
            _messageRepository.Verify(_ => _.MarkPongReplyForPingAsync(pong.BytesLen));
            
            _logger.VerifyAll();
            
            Assert.True(response.Success);
        }
    }
}