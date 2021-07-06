using System;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Fundamental;
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
        private readonly ParseFeatureFlags _parseFeatureFlags;

        public InitMessageServiceTests()
        {
            _repository = new ();
            _messageSender = new ();
            _features = new ();
            _parseFeatureFlags = new ();
            
            _sut = new InitMessageService(_repository.Object, _messageSender.Object,
                _features.Object, _parseFeatureFlags);
        }

        private void WithFeaturesThanAreSupportedLocally(PeerMessage<InitMessage> message)
        {
            _features.Setup(_ =>
                    _.ValidateRemoteFeatureAreCompatible(message.MessagePayload.Features, message.MessagePayload.GlobalFeatures))
                .Returns(true);
        }

        private static PeerMessage<InitMessage> NewRandomPeerMessage()
        {
            var message = new PeerMessage<InitMessage>
            (
                new PublicKey(RandomMessages.NewRandomPublicKey()),
                new BoltMessage
                {
                    Payload = new InitMessage
                    {
                        Features = RandomMessages.GetRandomByteArray(2),
                        GlobalFeatures = RandomMessages.GetRandomByteArray(1)
                    }
                }
            );
            return message;
        }

        [Fact]
        public void ProcessMessageAsyncThrowsWhenFeaturesAreNotSupportedLocally()
        {
            var message = NewRandomPeerMessage();

            Assert.ThrowsAsync<ArgumentException>(() => _sut.ProcessMessageAsync(message));
        }

        [Fact]
        public void ProcessMessageAddsPeerAndSendsResponse()
        {
            var message = NewRandomPeerMessage();

            var parsedFeatures = _parseFeatureFlags.ParseFeatures(message.MessagePayload.Features);
            
            WithFeaturesThanAreSupportedLocally(message);

            _sut.ProcessMessageAsync(message);

            _repository.Verify(_ => _.AddOrUpdatePeerAsync(It.Is<Peer>(p
                => (ulong)p.Featurs  == (ulong)parsedFeatures &&
                   p.NodeId.Equals(message.NodeId))));
        }
        
        [Fact]
        public void ProcessMessageUpdatesPeerAndSendsResponse()
        {
            var message = NewRandomPeerMessage();

            var parsedFeatures = _parseFeatureFlags.ParseFeatures(message.MessagePayload.Features);
            
            WithFeaturesThanAreSupportedLocally(message);

            var peer = new Peer
            {
                NodeId = message.NodeId,
                Featurs = (Features) RandomMessages.GetRandomNumberUInt16()
            };

            _repository.Setup(_ => _.TryGetPeerAsync(message.NodeId))
                .Returns(peer);

            _sut.ProcessMessageAsync(message);

            _repository.Verify(_ => _.AddOrUpdatePeerAsync(peer));
            
            Assert.Equal(peer.Featurs,parsedFeatures);
        }
    }
}