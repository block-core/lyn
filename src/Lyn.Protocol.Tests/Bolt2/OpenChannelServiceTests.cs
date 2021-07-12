using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt2;
using Lyn.Protocol.Bolt2.ChannelEstablishment;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
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
        private readonly Mock<IBoltFeatures> _features = new();
        private readonly Mock<ILogger<OpenChannelMessageService>> _logger = new();
        private readonly Mock<IRandomNumberGenerator> _randomNumberGenerator = new();
        private readonly LightningKeyDerivation _lightningKeyDerivation = new();
        private readonly Mock<IChannelCandidateRepository> _channelStateRepository = new();
        private readonly Mock<IChainConfigProvider> _chainConfigProvider = new();
        private readonly Mock<IChannelConfigProvider> _channelConfigProvider = new();
        private readonly Mock<IParseFeatureFlags> _parseFeatureFlags = new();
        private readonly Mock<ISecretStore> _secretProvider = new();

        private readonly Secret _randomSecret;

        public StartOpenChannelServiceTests()
        {
            _sut = new StartOpenChannelService(_logger.Object,
                _randomNumberGenerator.Object, _lightningKeyDerivation,
                _channelStateRepository.Object, _peerRepository.Object,
                _chainConfigProvider.Object, _channelConfigProvider.Object,
                _features.Object, _parseFeatureFlags.Object, _secretProvider.Object);

            _randomSecret = new Secret(RandomMessages.GetRandomByteArray(32));

            _secretProvider.Setup(_ => _.GetSeed()).Returns(new Secret(_randomSecret));
        }

        private (ChainParameters chainParameters, ChannelConfig channelConfig) WithExistingPeerAndChainParameters(CreateOpenChannelIn message)
        {
            var chainParameters = new ChainParameters { GenesisBlockhash = message.ChainHash };

            var channelConfig = new ChannelConfig
            {
                ChannelReserve = 1000,
                MaxAcceptedHtlcs = 5,
                ToSelfDelay = 1000,
                DustLimit = 100,
                HtlcMinimum = 100,
                MaxHtlcValueInFlight = 100000
            };

            _peerRepository.Setup(_ => _.TryGetPeerAsync(message.NodeId)).Returns(new Peer());
            _chainConfigProvider.Setup(_ => _.GetConfiguration(message.ChainHash)).Returns(chainParameters);
            _channelConfigProvider.Setup(_ => _.GetConfiguration(message.ChainHash)).Returns(channelConfig);

            return (chainParameters, channelConfig);
        }

        private static CreateOpenChannelIn NewStartChannelMessage()
        {
            var message = new CreateOpenChannelIn(
                new PublicKey(RandomMessages.NewRandomPublicKey()),
                RandomMessages.NewRandomUint256(),
                1000000,
                0, 500,
                false);
            return message;
        }

        private (PublicKey fundingKey, Basepoints basepoints) GetBasepointsFromSecret()
        {
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(_randomSecret);
            var fundingPubkey = _lightningKeyDerivation.PublicKeyFromPrivateKey(secrets.FundingPrivkey);
            Basepoints basepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);
            return (fundingPubkey, basepoints);
        }

        [Fact]
        public async Task StartOpenChannelSuccess()
        {
            var message = NewStartChannelMessage();

            var config = WithExistingPeerAndChainParameters(message);

            var result = await _sut.CreateOpenChannelAsync(message);

            // todo: dan add more checks

            Assert.IsType<OpenChannel>(result.Payload);
            OpenChannel openChannel = (OpenChannel)result.Payload;
            Assert.Equal(message.FundingAmount, openChannel.FundingSatoshis);

            var channelStates = new List<ChannelCandidate>();
            _channelStateRepository.Verify(_ =>
                _.CreateAsync(Capture.In(channelStates)), Times.Once);

            Assert.Single(channelStates);
            Assert.Equal(message.FundingAmount, channelStates.First().OpenChannel.FundingSatoshis);
            Assert.Equal(GetBasepointsFromSecret().fundingKey.ToString(), channelStates.First().OpenChannel.FundingPubkey.ToString());
            Assert.Equal(config.channelConfig.DustLimit, channelStates.First().OpenChannel.DustLimitSatoshis);
        }
    }
}