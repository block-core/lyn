using System;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Fundamental;
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

        private static PeerMessage<T> NewBoltMessage<T>(T message, PublicKey id)
        where T : MessagePayload
        {
            return new (id,new BoltMessage{Payload = message});
        }
        
        private static PeerMessage<PongMessage> WithPongBoltMessage()
        {
            return NewBoltMessage(new PongMessage
            {
                BytesLen = PingMessage.MAX_BYTES_LEN,
                Ignored = RandomMessages.GetRandomByteArray(PingMessage.MAX_BYTES_LEN)
            }, RandomMessages.NewRandomPublicKey());
        }
        
        [Fact]
        public async Task ProcessMessageAsyncWhenPingNotFoundDoesNotUpdateTheRepo()
        {
            var pong = WithPongBoltMessage();

            await _sut.ProcessMessageAsync(pong);
            
            _messageRepository.Verify(_ 
                    => _.MarkPongReplyForPingAsync(pong.NodeId,((PongMessage)pong.MessagePayload).Id)
            ,Times.Never);
        }
        
        [Fact]
        public async Task ProcessMessageAsyncUpdatesPingInRepoAndReturnsTrue()
        {
            var pong = WithPongBoltMessage();

            _messageRepository.Setup(_ => _.PendingPingExistsForIdAsync(pong.NodeId,pong.MessagePayload.Id))
                .Returns(() => new ValueTask<bool>(true))
                .Verifiable();
            
            await _sut.ProcessMessageAsync(pong);

            _messageRepository.Verify(_ => _.MarkPongReplyForPingAsync(pong.NodeId, pong.MessagePayload.Id));
        }
    }
}