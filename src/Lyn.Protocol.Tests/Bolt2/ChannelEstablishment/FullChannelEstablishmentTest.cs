using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization.Serializers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt2.ChannelEstablishment
{
    public class FullChannelEstablishmentTest
    {
        private StartOpenChannelService _openChannelService;
        private AcceptChannelMessageService _acceptChannelMessageService;
        private FundingSignedMessageService _fundingSignedMessageService;

        private Mock<IRandomNumberGenerator> _randomNumberGenerator;

        private InMemoryChannelCandidateRepository _candidateRepository;
        private InMemoryPeerRepository _peerRepository;

        private SerializationFactory _serializationFactory;
        private LightningScripts _lightningScripts;

        private LightningKeyDerivation _keyDerivation;

        private PublicKey _nodeId =
            Hex.FromString("0x02a8c859d0e7ce5b6d4f0c9d802d085342943d290f6dfb23b662a939d240f645f2");

        private static UInt256 _chainHash =
            new(Hex.FromString("06226e46111a0b59caaf126043eb5bbf28c34f3a5e332a1fc7b2b73cf188910f"));

        public FullChannelEstablishmentTest()
        {
            var loggerFactory = new LoggerFactory();
            _randomNumberGenerator = new Mock<IRandomNumberGenerator>();
            _candidateRepository = new InMemoryChannelCandidateRepository();
            _peerRepository = new InMemoryPeerRepository();

            var parsingFeatures = new ParseFeatureFlags();

            _serializationFactory =
                new SerializationFactory(new ServiceCollection().AddSerializationComponents().BuildServiceProvider());

            _lightningScripts = new LightningScripts();

            _keyDerivation = new LightningKeyDerivation();

            var lightningTransactions = new LightningTransactions(new Logger<LightningTransactions>(loggerFactory),
                _serializationFactory,
                _lightningScripts);

            var hashCalculator = new TransactionHashCalculator(new TransactionSerializer(
                new TransactionInputSerializer(new OutPointSerializer()),
                new TransactionOutputSerializer(),
                new TransactionWitnessSerializer(new TransactionWitnessComponentSerializer())));

            _openChannelService = new StartOpenChannelService(new Logger<OpenChannelMessageService>(loggerFactory),
                _randomNumberGenerator.Object,
                _keyDerivation,
                _candidateRepository,
                _peerRepository,
                new ChainConfigProvider(),
                new LynImplementedBoltFeatures(parsingFeatures),
                parsingFeatures,
                new SecretStore());

            _acceptChannelMessageService = new AcceptChannelMessageService(
                new Logger<AcceptChannelMessageService>(loggerFactory),
                lightningTransactions,
                hashCalculator,
                _lightningScripts,
                _keyDerivation,
                _candidateRepository,
                new ChainConfigProvider(),
                new SecretStore(),
                _peerRepository, new LynImplementedBoltFeatures(parsingFeatures));

            _fundingSignedMessageService = new FundingSignedMessageService(
                new Logger<FundingSignedMessageService>(loggerFactory),
                lightningTransactions,
                hashCalculator,
                _lightningScripts,
                _keyDerivation,
                _candidateRepository,
                new ChainConfigProvider(),
                new SecretStore(),
                _peerRepository,
                new LynImplementedBoltFeatures(parsingFeatures));


            _peerRepository.AddNewPeerAsync(new Peer
            {
                Featurs = Features.InitialRoutingSync | Features.VarOnionOptin | Features.GossipQueriesEx |
                          Features.PaymentSecret | (Features)2,
                Id = 0,
                GlobalFeatures = Features.OptionDataLossProtect,
                NodeId = _nodeId
            });

            _randomNumberGenerator.Setup(_ => _.GetBytes(32))
                .Returns(Hex.FromString("842508a1f5cbe1b5dda851def19edfe29671995c5670516e37259fa57b378a30"));
        }

        private readonly OpenChannel _expectedOpenChannel = new()
        {
            ChainHash = _chainHash,
            ChannelFlags = 1,
            ChannelReserveSatoshis = 160000,
            DelayedPaymentBasepoint =
                Hex.FromString("0x03786665023c26b0d5deb0678f7fa47049cb6cb6bd563b0864b3005c928adda40c"),
            DustLimitSatoshis = 100,
            FeeratePerKw = 1000,
            FirstPerCommitmentPoint =
                Hex.FromString("0x02c5183eaf73c13d377be535db654caccf343076de82dbd9003b6bd95969a3a061"),
            FundingPubkey = Hex.FromString("0x02b085ac037bb3b3ab6de81abf620e42df8d2a51ce4de2905b83bcd514e39f290f"),
            FundingSatoshis = 16000000,
            HtlcBasepoint = Hex.FromString("0x033336b311f22a7606122f4e7a6cf05460e008acb044e13f5718a5f32ccaa867a0"),
            HtlcMinimumMsat = 3000,
            MaxAcceptedHtlcs = 100,
            MaxHtlcValueInFlightMsat = 12000,
            PaymentBasepoint = Hex.FromString("0x020d4cbaa708e6fc9c4b3cb2792a9650724f9832fa018482c15cbfee337ec95fef"),
            PushMsat = 0,
            RevocationBasepoint =
                Hex.FromString("0x0203db8bcfcf69e4d565aa881873355fd6192899058e6b5b34a8ac469c1200ec36"),
            TemporaryChannelId =
                new UInt256(Hex.FromString("842508a1f5cbe1b5dda851def19edfe29671995c5670516e37259fa57b378a30")),
            ToSelfDelay = 2016,
        };

        private readonly AcceptChannel _acceptChannel = new()
        {
            ChannelReserveSatoshis = 160000,
            DelayedPaymentBasepoint =
                Hex.FromString("0x020a88f342257913656c9919851d57ade71c03ea145c4ca9f66e26d38e957baa92"),
            DustLimitSatoshis = 546,
            FirstPerCommitmentPoint =
                Hex.FromString("0x02cf2328c37f2fb5173f41260f270ce976fd53583d6766270395436c6810cfdb90"),
            FundingPubkey = Hex.FromString("0x02e5f015f999b1e3d568963f2dcdc73f32d8826f6cfac9b640d8568afb2eb9d548"),
            HtlcBasepoint = Hex.FromString("0x035f6b0612f5811e0f0d42d3c67e9b1482e3c48e1691f2e72c93834b5fbb3210fc"),
            HtlcMinimumMsat = 0,
            MaxAcceptedHtlcs = 30,
            MaxHtlcValueInFlightMsat = 5000000,
            MinimumDepth = 3,
            PaymentBasepoint = Hex.FromString("0x03acc1de45abeb2dae17979445d634b57d66431dafced39580133cf77efaf37c85"),
            RevocationBasepoint =
                Hex.FromString("0x03dd35f04d194350fa516c251c36bd3c6c890ff143e2233795a11bb9040b13e9de"),
            TemporaryChannelId =
                new UInt256(Hex.FromString("842508a1f5cbe1b5dda851def19edfe29671995c5670516e37259fa57b378a30")),
            ToSelfDelay = 720
        };

        private readonly FundingCreated _expectedFundingCreated = new()
        {
            FundingOutputIndex = 0,
            FundingTxid =
                new UInt256(Hex.FromString("0x40b4b05dc32c0862fb1026876a7acfc2b9c66da2bb0284367b06404e1198ffca")),
            Signature = Hex.FromString(
                "0xb809fee80948c415d08e8a113168d2d507cb9e78d391dbde838212099b5b62752ce0883e1be4e99ba18bc947ab31ce9d44c6bc88fe0362eaeb4e0914d6063ed1"),
            TemporaryChannelId =
                new UInt256(Hex.FromString("842508a1f5cbe1b5dda851def19edfe29671995c5670516e37259fa57b378a30"))
        };

        private readonly FundingSigned _fundingSigned = new ()
        {
            ChannelId = new UInt256(
                Hex.FromString("0x40b4b05dc32c0862fb1026876a7acfc2b9c66da2bb0284367b06404e1198ffca")),
            Signature = Hex.FromString(
                "0x7dda8bb401b0236edb0e97de360626ba64c8eebffc1399ca4ce17831c196d8b3232c4eea5db33d60811c1f6d131b4f4cd9a330d0f50dcb063daaf648e40a9b30")
        };

        [Fact]
        public async Task FullChannelEstablishmentScenarioCompletesSuccessfully()
        {
            var openChannelResponse = await _openChannelService.CreateOpenChannelAsync(new CreateOpenChannelIn(_nodeId,
                _chainHash, 16000000, 0, 1000, false));

            openChannelResponse.Payload.Should()
                .BeEquivalentTo(_expectedOpenChannel);

            var acceptMessageResponse = await _acceptChannelMessageService.ProcessMessageAsync(
                new PeerMessage<AcceptChannel>(_nodeId,
                    new BoltMessage { Payload = _acceptChannel }));

            acceptMessageResponse.Success.Should().BeTrue();
            acceptMessageResponse.ResponseMessages.Should()
                .ContainSingle()
                .Which.Payload.Should()
                .BeEquivalentTo(_expectedFundingCreated);

            var response = await _fundingSignedMessageService.ProcessMessageAsync(
                new PeerMessage<FundingSigned>(_nodeId, new BoltMessage { Payload = _fundingSigned }));

            response.Success.Should().BeTrue();
        }
    }
}