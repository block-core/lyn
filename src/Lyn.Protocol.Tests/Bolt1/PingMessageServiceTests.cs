using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Common;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;
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
        private readonly Mock<IBoltMessageSender<PongMessage>> _boltMessageSender;

        private DateTime _utcNow;
        private ushort _uint16;

        public PingMessageServiceTests()
        {
            _logger = new Mock<ILogger<PingMessageService>>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();
            _randomNumberGenerator = new Mock<IRandomNumberGenerator>();
            _messageRepository = new Mock<IPingPongMessageRepository>();
            _boltMessageSender = new Mock<IBoltMessageSender<PongMessage>>();

            _sut = new PingMessageService(_logger.Object, _dateTimeProvider.Object,
                _randomNumberGenerator.Object, _messageRepository.Object, _boltMessageSender.Object);

            _utcNow = DateTime.UtcNow;

            _dateTimeProvider.Setup(_ => _.GetUtcNow())
                .Returns(() => _utcNow);

            _uint16 = RandomMessages.GetRandomNumberUInt16();
            
            _randomNumberGenerator.Setup(_ => _.GetUint16())
                .Returns(() => _uint16);
        }

        private static PeerMessage<PingMessage> WithPingBoltMessage(ushort numPongBytes)
        {
            return new PeerMessage<PingMessage>
            {
                Message = new PingMessage
                {
                    BytesLen = PingMessage.MAX_BYTES_LEN,
                    Ignored = RandomMessages.GetRandomByteArray(PingMessage.MAX_BYTES_LEN),
                    NumPongBytes = numPongBytes
                },
                NodeId = RandomMessages.NewRandomPublicKey()
            };
        }
        
        [Fact]
        public async Task ProcessMessageAsyncWhenMessageIsLongerThanAllowedFailsTheMessage()
        {
            var message = WithPingBoltMessage(PingMessage.MAX_BYTES_LEN + 1);

            await _sut.ProcessMessageAsync(message);
            
            _boltMessageSender.VerifyNoOtherCalls();
        }

        
        [Fact]
        public async Task ProcessMessageAsyncReturnsSuccessWithAPongMessage()
        {
            var message = WithPingBoltMessage(PingMessage.MAX_BYTES_LEN);

            await _sut.ProcessMessageAsync(message);
            
            _boltMessageSender.Verify(_ => _.SendMessageAsync(It.Is<PeerMessage<PongMessage>>(_ => 
                _.Message.BytesLen == message.Message.NumPongBytes && 
                _.Message.BytesLen == message.Message.Ignored!.Length)));
        }
        
        [Fact]
        public async Task ProcessMessageAsyncThrowsIfPingWasReceivedBeforeTheAllowedTime()
        {
            var message = WithPingBoltMessage(PingMessage.MAX_BYTES_LEN);

            await _sut.ProcessMessageAsync(message);
 
            await Assert.ThrowsAsync<ProtocolViolationException>(() 
                => _sut.ProcessMessageAsync(message));
        }

        [Fact]
        public async Task CreateNewMessageAsyncReturnsPingMessageAndStoresInRepo()
        {
            var nodeId = RandomMessages.NewRandomPublicKey();
            
            var result = await _sut.CreateNewMessageAsync(nodeId);

            _messageRepository.Verify(_ => _.AddPingMessageAsync(nodeId,_utcNow, result));

            Assert.Equal(PingMessage.MAX_BYTES_LEN - _uint16 % PingMessage.MAX_BYTES_LEN, result.NumPongBytes);
        }
        
        [Fact]
        public async Task CreateNewMessageAsyncWhenThePingLengthExistsCreatesNewOne()
        {
            var nodeId = RandomMessages.NewRandomPublicKey();

            _messageRepository.SetupSequence(_ 
                    => _.PendingPingExistsForIdAsync(nodeId, (ushort)(_uint16 % PingMessage.MAX_BYTES_LEN)))
                .Returns(() => new ValueTask<bool>(true))
                .Returns(() => new ValueTask<bool>(false));
            
            var result = await _sut.CreateNewMessageAsync(nodeId);

            _messageRepository.Verify(_ => _.AddPingMessageAsync(nodeId, _utcNow, result));

            _messageRepository.VerifyAll();
            
            Assert.Equal(PingMessage.MAX_BYTES_LEN - _uint16 % PingMessage.MAX_BYTES_LEN, result.NumPongBytes);
        }
    }
}