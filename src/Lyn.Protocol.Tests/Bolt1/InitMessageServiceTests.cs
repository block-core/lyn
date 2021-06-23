using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt.Messages;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt1
{
    public class InitMessageServiceTests
    {
        private InitMessageService _sut;

        private readonly Mock<IPeerRepository> _repository;
        private readonly Mock<IBoltMessageSender<InitMessage>> _messageSender;
        private readonly Mock<IBoltFeatures> _features;
        private readonly Mock<IParseFeatureFlags> _featureFlags;

        public InitMessageServiceTests()
        {
            _repository = new Mock<IPeerRepository>();
            _messageSender = new Mock<IBoltMessageSender<InitMessage>>();
            _features = new Mock<IBoltFeatures>();
            _featureFlags = new Mock<IParseFeatureFlags>();

            _sut = new InitMessageService(_repository.Object, _messageSender.Object,
                _features.Object, _featureFlags.Object);
        }

        [Fact]
        public void ProcessMessageAddsPeerAndSendsResponse()
        {
            var message = new PeerMessage<InitMessage>
            {
                Message = new InitMessage
                {
                    Features = RandomMessages.GetRandomByteArray(2),
                    GlobalFeatures = RandomMessages.GetRandomByteArray(1)
                },
                NodeId = RandomMessages.NewRandomPublicKey()
            };

            _sut.ProcessMessageAsync(message);

            _repository.Verify(_ => _.AddNewPeerAsync(It.Is<Peer>(p
                => p.Featurs.Equals(message.Message.Features) &&
                   p.GlobalFeatures.Equals(message.Message.GlobalFeatures) &&
                   p.NodeId.Equals(message.NodeId))));

            _messageSender.Verify(_ =>
                _.SendMessageAsync(It.IsAny<PeerMessage<InitMessage>>())); // TODO make the test more detailed
        }
    }
}