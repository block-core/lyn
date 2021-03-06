using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
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

        private DateTime _utcNow;
        private ushort _uint16;

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

            _uint16 = RandomMessages.GetRandomNumberUInt16();

            _randomNumberGenerator.Setup(_ => _.GetUint16())
                .Returns(() => _uint16);
        }

        private static PeerMessage<PingMessage> WithPingBoltMessage(ushort numPongBytes)
        {
            return new
            (
                RandomMessages.NewRandomPublicKey(),
                new BoltMessage
                {
                    Payload = new PingMessage
                    {
                        BytesLen = PingMessage.MAX_BYTES_LEN,
                        Ignored = RandomMessages.GetRandomByteArray(PingMessage.MAX_BYTES_LEN),
                        NumPongBytes = numPongBytes
                    }
                }
            );
        }

        private void ThanTheMessageWasAddedToTheRepository(PublicKey? nodeId)
        {
            _messageRepository.Verify(_ => _.AddPingMessageAsync(nodeId, _utcNow,
                It.Is<PingMessage>(_ => _.BytesLen == _uint16 % PingMessage.MAX_BYTES_LEN)));
        }

        private void ThanTheMessageForPingWasReturnedFromTheService(MessageProcessingOutput output)
        {
            output.Success.Should().BeTrue();
            output.ResponseMessages.Should().ContainSingle()
                .Which.Payload.Should().BeEquivalentTo(new PingMessage((ushort)(_uint16 % PingMessage.MAX_BYTES_LEN)));
        }
        
        [Fact]
        public async Task ProcessMessageAsyncWhenMessageIsLongerThanAllowedFailsTheMessage()
        {
            var message = WithPingBoltMessage(PingMessage.MAX_BYTES_LEN + 1);

            var result =  await _sut.ProcessMessageAsync(message);

            result.Success.Should().BeFalse();
            result.ResponseMessages.Should().BeNull();
        }


        [Fact]
        public async Task ProcessMessageAsyncReturnsSuccessWithAPongMessage()
        {
            var message = WithPingBoltMessage(PingMessage.MAX_BYTES_LEN);

            var result = await _sut.ProcessMessageAsync(message);

            result.Should().NotBeNull();

            result.Success.Should().BeTrue();
            
            result.ResponseMessages.Should()
                .ContainSingle()
                .Which.Payload.Should()
                .BeEquivalentTo(new PongMessage
                {
                    BytesLen = message.MessagePayload.NumPongBytes,
                    Ignored = new byte[message.MessagePayload.NumPongBytes]
                });
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

            var result = await _sut.GeneratePingMessageAsync(nodeId, CancellationToken.None);

            ThanTheMessageWasAddedToTheRepository(nodeId);

            ThanTheMessageForPingWasReturnedFromTheService(result);
        }


        [Fact]
        public async Task CreateNewMessageAsyncWhenThePingLengthExistsCreatesNewOne()
        {
            var nodeId = RandomMessages.NewRandomPublicKey();

            _messageRepository.SetupSequence(_
                    => _.PendingPingExistsForIdAsync(nodeId, (ushort) (_uint16 % PingMessage.MAX_BYTES_LEN)))
                .Returns(() => new ValueTask<bool>(true))
                .Returns(() => new ValueTask<bool>(false));

            var result = await _sut.GeneratePingMessageAsync(nodeId, CancellationToken.None);

            ThanTheMessageWasAddedToTheRepository(nodeId);

            _messageRepository.VerifyAll();

            ThanTheMessageForPingWasReturnedFromTheService(result);
        }
    }
}