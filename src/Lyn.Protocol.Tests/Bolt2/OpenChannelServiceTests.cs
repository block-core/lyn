using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt2;
using Lyn.Protocol.Bolt2.ChannelEstablishment;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Connection;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt2
{
    public class StartOpenChannelServiceTests
    {
        private StartOpenChannelService _sut;

        private readonly Mock<IPeerRepository> _peerRepository = new();
        private readonly Mock<IBoltMessageSender<OpenChannel>> _messageSender = new();
        private readonly Mock<IBoltFeatures> _features = new();
        private readonly Mock<ILogger<OpenChannelMessageService>> _logger = new();
        private readonly Mock<IRandomNumberGenerator> _randomNumberGenerator = new();
        private readonly LightningKeyDerivation _lightningKeyDerivation = new();
        private readonly Mock<IChannelStateRepository> _channelStateRepository = new();
        private readonly Mock<IChainConfigProvider> _chainConfigProvider = new();
        private readonly Mock<IChannelConfigProvider> _channelConfigProvider = new();
        private readonly Mock<IParseFeatureFlags> _parseFeatureFlags = new();
        private readonly Mock<ISecretProvider> _secretProvider = new();

        public StartOpenChannelServiceTests()
        {
            _sut = new StartOpenChannelService(_logger.Object, _messageSender.Object,
                _randomNumberGenerator.Object, _lightningKeyDerivation,
                _channelStateRepository.Object, _peerRepository.Object,
                _chainConfigProvider.Object, _channelConfigProvider.Object,
                _features.Object, _parseFeatureFlags.Object, _secretProvider.Object);

            _secretProvider.Setup(_ => _.GetSeed()).Returns(new Secret(RandomMessages.GetRandomByteArray(32)));
        }

        private void WithExistingPeerAndChainParameters(StartOpenChannelIn message)
        {
            _peerRepository.Setup(_ => _.GetPeer(message.NodeId)).Returns(new Peer());
            _chainConfigProvider.Setup(_ => _.GetConfiguration(message.ChainHash)).Returns(new ChainParameters { GenesisBlockhash = message.ChainHash });
            _channelConfigProvider.Setup(_ => _.GetConfiguration(message.ChainHash)).Returns(new ChannelConfig());
        }

        private static StartOpenChannelIn? NewStartChannelMessage()
        {
            var message = new StartOpenChannelIn(
                new PublicKey(RandomMessages.NewRandomPublicKey()),
                RandomMessages.NewRandomUint256(),
                1000000,
                0, 500,
                false);
            return message;
        }

        [Fact]
        public async Task StartOpenChannelSuccess()
        {
            var message = NewStartChannelMessage();

            WithExistingPeerAndChainParameters(message);

            await _sut.StartOpenChannelAsync(message);

            _channelStateRepository.Verify(_ =>
                _.Create(It.IsAny<ChannelState>()), Times.Once);

            _messageSender.Verify(_ =>
                _.SendMessageAsync(It.IsAny<PeerMessage<OpenChannel>>()), Times.Once);
        }
    }
}