using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Types.Bolt.Messages;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt1
{
    public class ErrorMessageServiceTests
    {
        private ErrorMessageService _sut;

        private readonly Mock<ILogger<ErrorMessageService>> _logger;
        private readonly Mock<IPeerRepository> _repository;
        
        
        public ErrorMessageServiceTests()
        {
            _logger = new Mock<ILogger<ErrorMessageService>>();
            _repository = new Mock<IPeerRepository>();
            
            _sut = new ErrorMessageService(_logger.Object,_repository.Object);
        }

        [Fact]
        public async Task ProcessMessageAsyncStoresErrorReturnsSuccess()
        {
            var message = new ErrorMessage
            {
                ChannelId = RandomMessages.NewRandomChannelId(),
                Len = RandomMessages.GetRandomNumberUInt16(),
                Data = RandomMessages.GetRandomByteArray(64) //64 just to keep it small it actually is a text message 
            };

            var result = await _sut.ProcessMessageAsync(message,CancellationToken.None);
            
            _repository.Verify(_ => _.AddErrorMessageToPeerAsync());
            
            Assert.True(result.Success);
        }
    }
}