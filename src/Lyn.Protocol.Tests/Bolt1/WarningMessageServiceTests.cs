using System;
using System.Text;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt1
{
    public class WarningMessageServiceTests
    {
        private WarningMessageService _sut;

        private readonly Mock<ILogger<WarningMessageService>> _logger;
        private readonly Mock<IPeerRepository> _repository;


        public WarningMessageServiceTests()
        {
            _logger = new Mock<ILogger<WarningMessageService>>();
            _repository = new Mock<IPeerRepository>();
            
            _sut = new WarningMessageService(_logger.Object,_repository.Object);
        }

        [Fact]
        public async Task ProcessMessageAsyncStoresErrorReturnsSuccess()
        {
            var expectedWarningData = RandomMessages.GetRandomByteArray(64);//64 just to keep it small it actually is a text message

            var expectedWarningMessage = Encoding.ASCII.GetString(expectedWarningData);

            UInt256 channel = RandomMessages.NewRandomUint256();
            
            var message = new PeerMessage<WarningMessage>
            (RandomMessages.NewRandomPublicKey(),
                new BoltMessage
                {
                    Payload = new WarningMessage
                    {
                        ChannelId = channel,
                        Len = RandomMessages.GetRandomNumberUInt16(),
                        Data = expectedWarningData  
                    }
                });

            await _sut.ProcessMessageAsync(message);
            
            _repository.Verify(_ => _.AddErrorMessageToPeerAsync(message.NodeId,It.Is<PeerCommunicationIssue>(p 
            => p.ChannelId == message.MessagePayload.ChannelId &&
               p.MessageText == Encoding.ASCII.GetString(message.MessagePayload.Data) &&
               p.MessageType == message.Message.Type)));
        }
    }
}