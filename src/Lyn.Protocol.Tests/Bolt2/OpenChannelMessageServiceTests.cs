using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt2
{
    public class OpenChannelMessageServiceTests
    {
        private OpenChannelMessageService _sut;

        private readonly Mock<IPeerRepository> _peerRepository = new();
        private readonly Mock<IBoltFeatures> _features = new();
        private readonly Mock<ILogger<OpenChannelMessageService>> _logger = new();
        private readonly Mock<ILightningTransactions> _lightningTransactions = new();
        private readonly LightningKeyDerivation _lightningKeyDerivation = new();
        private readonly Mock<IChannelCandidateRepository> _channelCandidateRepository = new();
        private readonly Mock<IChainConfigProvider> _chainConfigProvider = new();
        private readonly Mock<ISecretStore> _secretProvider = new();

        private readonly Secret _randomSecret;

        public OpenChannelMessageServiceTests()
        {
            _sut = new OpenChannelMessageService(_logger.Object,
                _lightningTransactions.Object, _lightningKeyDerivation,
                _channelCandidateRepository.Object, _chainConfigProvider.Object,
                _peerRepository.Object, _secretProvider.Object, _features.Object);

            _randomSecret = new Secret(RandomMessages.GetRandomByteArray(32));

            _secretProvider.Setup(_ => _.GetSeed()).Returns(new Secret(_randomSecret));
        }

        private ChainParameters WithExistingPeerAndChainParameters(PeerMessage<OpenChannel> message)
        {
            var chainParameters = new ChainParameters
            {
                Chainhash = message.MessagePayload.ChainHash,
                ChannelConfig = new ChannelConfig
                {
                    ChannelReserve = 1000,
                    MaxAcceptedHtlcs = 5,
                    ToSelfDelay = 1000,
                    DustLimit = 5,
                    HtlcMinimum = 100,
                    MaxHtlcValueInFlight = 100000
                },
                ChannelBoundariesConfig = new ChannelBoundariesConfig
                {
                    MinEffectiveHtlcCapacity = 100,
                    AllowPrivateChannels = true,
                    MinimumDepth = 6,
                    ChannelReservePercentage = 0.1M,
                    MaxToSelfDelay = 200,
                    TooLargeFeeratePerKw = 500,
                    TooLowFeeratePerKw = 50
                },
            };

            _peerRepository.Setup(_ => _.TryGetPeerAsync(message.NodeId)).Returns(new Peer());
            _chainConfigProvider.Setup(_ => _.GetConfiguration(message.MessagePayload.ChainHash)).Returns(chainParameters);
            _lightningTransactions.Setup(_ => _.GetBaseFee(It.IsAny<Satoshis>(), It.IsAny<bool>(), 0)).Returns(100);

            return chainParameters;
        }

        private PeerMessage<OpenChannel> NewStartChannelMessage()
        {
            var message = new PeerMessage<OpenChannel>(new PublicKey(RandomMessages.NewRandomPublicKey()),
                new BoltMessage
                {
                    Payload = new OpenChannel
                    {
                        ChainHash = RandomMessages.NewRandomUint256(),
                        ChannelReserveSatoshis = 100,
                        FeeratePerKw = 100,
                        HtlcMinimumMsat = 50,
                        MaxAcceptedHtlcs = 1,
                        DustLimitSatoshis = 10,
                        PushMsat = 0,
                        ToSelfDelay = 100,
                        FundingPubkey = RandomMessages.NewRandomPublicKey(),
                        RevocationBasepoint = RandomMessages.NewRandomPublicKey(),
                        HtlcBasepoint = RandomMessages.NewRandomPublicKey(),
                        PaymentBasepoint = RandomMessages.NewRandomPublicKey(),
                        DelayedPaymentBasepoint = RandomMessages.NewRandomPublicKey(),
                        FundingSatoshis = 1000000
                    }
                });

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

            var result = await _sut.ProcessMessageAsync(message);

            // todo: dan add more checks

            Assert.NotNull(result);
            Assert.Single(result.ResponseMessages);
            Assert.IsType<AcceptChannel>(result.ResponseMessages.First().Payload);
            AcceptChannel acceptChannel = (AcceptChannel)result.ResponseMessages.First().Payload;
            Assert.Equal(GetBasepointsFromSecret().fundingKey.ToString(), acceptChannel.FundingPubkey.ToString());

            var channelStates = new List<ChannelCandidate>();
            _channelCandidateRepository.Verify(_ =>
                _.CreateAsync(Capture.In(channelStates)), Times.Once);

            Assert.Single(channelStates);
            Assert.Equal(message.MessagePayload.FundingPubkey, channelStates.First().OpenChannel.FundingPubkey);
            Assert.Equal(GetBasepointsFromSecret().fundingKey.ToString(), channelStates.First().AcceptChannel.FundingPubkey.ToString());
            Assert.Equal(config.ChannelConfig.DustLimit, channelStates.First().AcceptChannel.DustLimitSatoshis);
        }
    }
}