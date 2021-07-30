using System.Buffers;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Serialization.Serializers;
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

        [Fact]
        public void test()
        {
            var serializesr = new TransactionSerializer(new TransactionInputSerializer(new OutPointSerializer()),new TransactionOutputSerializer(),new TransactionWitnessSerializer(new TransactionWitnessComponentSerializer()));

            var bytes = Hex.FromString(
                "02000000000101ac2c641df0fad2cbd74e01da0af1c9ae89d73c1747ddd33df6c96cef2e2565f800000000009a4cf180012c21f40000000000160014a5dcd38b4493c0cf561e630a2f2399f3bd4942600400473044022072d47d9eefdf847645d859738ee2c9138a18ff7585a05935dc87470651b8fefc022027d557a94ead04e0fbb6c6c9286eed8fed20b91d171f7581700d93f0dc151727014730440220618e4f3d455fd61ef2c92888bcf65039864ef6f14d402707b6fd79098d2d90af022074e9381bdb70a58157077ab43aab5084ae6402640a2ce043d4d997a1c79fc04701475221033c3f33e23b2b4afdd58c8b2bc5847cf3fbdd433b347e2681eb9f25c00ac3c216210393170065a84271242134852b6336de92d732e17253733ffc6d49be04b1f4329a52aefba2bd20");
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            
            var pb = serializesr.Deserialize(ref reader);
       }
    }
}