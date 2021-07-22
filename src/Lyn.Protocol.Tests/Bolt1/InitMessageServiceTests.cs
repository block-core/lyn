using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt1.Messages.TlvRecords;
using Lyn.Protocol.Bolt7;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Fundamental;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt1
{
    public class InitMessageServiceTests
    {
        private InitMessageService _sut;

        private readonly Mock<IPeerRepository> _repository;
        private readonly Mock<IBoltFeatures> _features;
        private readonly ParseFeatureFlags _parseFeatureFlags;
        private readonly Mock<IGossipRepository> _gossipRepository;

        public InitMessageServiceTests()
        {
            _repository = new ();
            _features = new ();
            _parseFeatureFlags = new ();
            _gossipRepository = new();
            
            _sut = new InitMessageService(_repository.Object,
                _features.Object, _parseFeatureFlags, _gossipRepository.Object);
        }

        private void WithFeaturesThanAreSupportedLocally(PeerMessage<InitMessage> message)
        {
            _features.Setup(_ =>
                    _.ValidateRemoteFeatureAreCompatible(message.MessagePayload.Features, message.MessagePayload.GlobalFeatures))
                .Returns(true);
        }
        
        private byte[] WithGetSupportedFeaturesDefined()
        {
            var bytes = RandomMessages.GetRandomByteArray(2);
            
            _features.Setup(_ => _.GetSupportedFeatures())
                .Returns(bytes);

            return bytes;
        }
        
        private byte[] WithGetSupportedGlobalFeaturesDefined()
        {
            var bytes = RandomMessages.GetRandomByteArray(2);
            
            _features.Setup(_ => _.GetSupportedGlobalFeatures())
                .Returns(bytes);

            return bytes;
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
        
        private static void ThanTheResultIsBoltMessageWithInit(MessageProcessingOutput result, byte[] globalFeatures,
            byte[] features)
        {
            result.Success.Should().BeTrue();
            result.ResponseMessages.Should()
                .ContainSingle()
                .Which.Should()
                .BeEquivalentTo(new BoltMessage
                {
                    Payload = new InitMessage
                    {
                        GlobalFeatures = globalFeatures,
                        Features = features,
                    },
                    Extension = new TlVStream
                    {
                        Records = new List<TlvRecord>
                        {
                            new NetworksTlvRecord {Type = 1, Payload = ChainHashes.BitcoinSignet.GetBytes().ToArray(), Size = 32}
                        }
                    }
                });
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
        public void ProcessMessageAddsNodeToGossipRepository()
        {
            var message = NewRandomPeerMessage();

            _parseFeatureFlags.ParseFeatures(message.MessagePayload.Features);
            
            WithFeaturesThanAreSupportedLocally(message);

            _sut.ProcessMessageAsync(message);

            _gossipRepository.Verify(_ => _.AddNodeAsync(It.Is<GossipNode>(_ 
            => _.Id == message.NodeId)));
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

        [Fact]
        public async Task GenerateInitAsyncSavesPeerAndReturnsRespond()
        {
            var nodeId = RandomMessages.NewRandomPublicKey();

            var features = WithGetSupportedFeaturesDefined();
            var globalFeatures = WithGetSupportedGlobalFeaturesDefined();
            
            var result = await _sut.GenerateInitAsync(nodeId, CancellationToken.None);

            _repository.Verify(_ => _.AddNewPeerAsync(It.Is<Peer>(p
                => p.NodeId == nodeId)));
            
            ThanTheResultIsBoltMessageWithInit(result, globalFeatures, features);
        }



        [Fact]
        public async Task GenerateInitAsyncWithNewPeerOnlyReturnsRespond()
        {
            var nodeId = RandomMessages.NewRandomPublicKey();

            var features = WithGetSupportedFeaturesDefined();
            var globalFeatures = WithGetSupportedGlobalFeaturesDefined();

            _repository.Setup(_ => _.PeerExists(nodeId))
                .Returns(true);
            
            var result = await _sut.GenerateInitAsync(nodeId, CancellationToken.None);

            ThanTheResultIsBoltMessageWithInit(result, globalFeatures, features);
        }
    }
}