using System.Linq;
using FluentAssertions;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
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
using Lyn.Types.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt2
{
    public class AcceptChannelMessageServiceTests
    {
        private AcceptChannelMessageService _sut;

        private InMemoryChannelCandidateRepository _candidateRepository;
        private readonly Mock<ISecretStore> _store;

        private SerializationFactory serializationFactory;

        private InMemoryPeerRepository inMemoryPeerRepository;
        private readonly Mock<IBoltFeatures> _features = new();

        public AcceptChannelMessageServiceTests()
        {
            _candidateRepository = new InMemoryChannelCandidateRepository();

            _store = new Mock<ISecretStore>();

            var ci = new ServiceCollection().AddSerializationComponents().BuildServiceProvider();
            serializationFactory = new SerializationFactory(ci);

            inMemoryPeerRepository = new InMemoryPeerRepository();

            _sut = new AcceptChannelMessageService(new Logger<AcceptChannelMessageService>(new LoggerFactory()),
            new LightningTransactions(new Logger<LightningTransactions>(new LoggerFactory()), serializationFactory,
                new LightningScripts()),
            new TransactionHashCalculator(ci.GetService<IProtocolTypeSerializer<Transaction>>()),
            new LightningScripts(),
            new LightningKeyDerivation(),
            _candidateRepository,
            new ChainConfigProvider(),
            _store.Object,
            inMemoryPeerRepository,
            _features.Object);
        }

        [Fact]
        public void TestGeneratingFundingCreated()
        {
            var acceptChannel = new AcceptChannel
            {
                FundingPubkey = Hex.FromString("0x03100a0e62903ae4a67902241f41590bce4f925e3676a41667388c8b2da446b25f"),
                HtlcBasepoint = Hex.FromString("0x02615c1607d46592c11c3dc7e3215cf05fbf1c417702ae88be500c22a30c084de0"),
                MinimumDepth = 3,
                PaymentBasepoint =
                    Hex.FromString("0x024e50f1f547d9cb1150fd19b252e2bc56966f3000b331f6ff12c9736f041e9e02"),
                RevocationBasepoint =
                    Hex.FromString("0x02a6ea134b2fda74bdb23de5b1f060c96d8c6d0bf405a102ed65af5e854cda1879"),
                ChannelReserveSatoshis = 160000,
                DelayedPaymentBasepoint =
                    Hex.FromString("0x025b646fef398cffd0a2a4584ae604a788b14ff57350a0ce548d1825af3d762db7"),
                DustLimitSatoshis = 546,
                HtlcMinimumMsat = 1,
                MaxAcceptedHtlcs = 30,
                TemporaryChannelId =
                    new UInt256(Hex.FromString("431edbcc612dffd3ede22b84beca344a810cc360b94b22601d165f584159c085")),
                ToSelfDelay = 720,
                FirstPerCommitmentPoint =
                    Hex.FromString("0x03cf020f4341d3ef7af94b49f13d5d234ed82887529f122bb6d27d2ba645ac4340"),
                MaxHtlcValueInFlightMsat = 5000000000
            };

            var openChannel = new OpenChannel
            {
                ChainHash = new UInt256(
                    Hex.FromString("06226e46111a0b59caaf126043eb5bbf28c34f3a5e332a1fc7b2b73cf188910f")),
                ChannelFlags = 0,
                FundingPubkey = Hex.FromString("0x02f46a97ef60cf4f4e09882439683346f71b2edc07cbe64a5d57d0d727e3fed231"),
                FundingSatoshis = 16000000,
                HtlcBasepoint = Hex.FromString("0x033318fa8caf6f23df0524b5ceb45b4f364526dca74ab3a85cdd11554eae93ab86"),
                PaymentBasepoint =
                    Hex.FromString("0x03e23c94d3e266ccf10f930fea0e59c2267c649866c0ffd71dcd295f9135712064"),
                PushMsat = 0,
                RevocationBasepoint =
                    Hex.FromString("0x02be67385cac45c599e2ce7e6e59a518b01e36f1b58cab5410031ba00bed4bfb21"),
                ChannelReserveSatoshis = 160000,
                DelayedPaymentBasepoint =
                    Hex.FromString("0x02f4b187ffc1fd574493726193750f06adde9c9d0b2b31d9d006a5c9bcf33139d6"),
                DustLimitSatoshis = 100,
                FeeratePerKw = 1000,
                HtlcMinimumMsat = 30000,
                MaxAcceptedHtlcs = 100,
                TemporaryChannelId =
                    new UInt256(Hex.FromString("431edbcc612dffd3ede22b84beca344a810cc360b94b22601d165f584159c085")),
                ToSelfDelay = 2016,
                FirstPerCommitmentPoint =
                    Hex.FromString("0x03a25b530f11377cb644fab06bb363aaaa70e0f78a785ee057ac7c1519ae7dee41"),
                MaxHtlcValueInFlightMsat = 12000000
            };

            var seed = new Secret(Hex.FromString("0x179c322acdd402a29131762db49ff5011916a8e86b752dbae2a9a8d664cf6ce0"));

            //_sut.ProcessMessageAsync();
        }

        [Fact]
        public void TestFromSeed()
        {
            var remp = TransactionHelper.ParseToString(
                serializationFactory.Deserialize<Transaction>(
                    Hex.FromString("0x0200000001f5135fd3849b8cfdac1acb2431eca8dd0a2510e2f3892541f0448195dcd244ad00000000004436e28001a086010000000000160014ad4f47c8e90b86df314005ef048fd24ba349988bf3d89720")));

            var locp = TransactionHelper.ParseToString(
                serializationFactory.Deserialize<Transaction>(
                    Hex.FromString("0x0200000001702c4833a9095956c1d330eb230d02f8b0eb100c9ac25acf5da797a2fecbfe270000000000cee25b8001c78501000000000022002098ece0e8073947f9f0bb3c20dd1611c52a88e54a893bb9a02c34b300ed0ad8cdaa569e20")));

            var seed = new Secret(Hex.FromString("0x5e46094b865e688419c3bec96de09da2f1e40fd71f79588c34502a12332ef074"));

            var nodeId = Hex.FromString("03702309a58b6067e51a93a213d72a9bdebf5f5f03960d9ff2bc311e301e4ba999");

            var rawAcceptChannel = "0200000001d573e708ed7e600ed2c3f6bd6c97ebc9eec9f3c3640897479be68028d2c3e9fb0000000000957f908001c785010000000000160014133ce10abfb75c2c9a2556f7db0514dfb4a17e077e30fb20";

            var deserializedAcceptChannel = serializationFactory.Deserialize<AcceptChannel>(Hex.FromString(rawAcceptChannel));

            _store.Setup(_ => _.GetSeed())
                .Returns(seed);

            inMemoryPeerRepository.AddNewPeerAsync(new Peer
            {
                Featurs = 0,
                NodeId = nodeId
            }).GetAwaiter().GetResult();

            var generator = new Mock<IRandomNumberGenerator>();

            generator.Setup(_ => _.GetBytes(32))
                .Returns(deserializedAcceptChannel.TemporaryChannelId.GetBytes().ToArray());

            var startOpenChannelService = new StartOpenChannelService(
                new Logger<OpenChannelMessageService>(new LoggerFactory()),
                generator.Object,
                new LightningKeyDerivation(),
                _candidateRepository,
                inMemoryPeerRepository,
                new ChainConfigProvider(),
                new LynImplementedBoltFeatures(new ParseFeatureFlags()),
                new ParseFeatureFlags(),
                _store.Object);

            var openChannelResponse = startOpenChannelService.CreateOpenChannelAsync(new CreateOpenChannelIn(
                nodeId,
                new UInt256(Hex.FromString("0f9188f13cb7b2c71f2a335e3a4fc328bf5beb436012afca590b1a11466e2206")),
                16000000, 0, 1000, true))
                .GetAwaiter().GetResult();

            var expectedTransaction = Hex.FromString("020000000001017e3e2666942003b00119281be2665503f96543f099bb814a5454644796f08a02000000000035638880012c21f40000000000160014ee666c47268bf2571e1ffe51cd7c7a262f186ca6040047304402203a77357de8c239b83a6612a7cbcfb2d2041770a6ede0921761646bb057fe0e3d0220046662c9eb643c78b487e5b87014f3350a5f3cd09d718be5f9bd22d919f0e8130147304402207c45f65f5eb852f8e3861c2e490597a37497d1382b0455e5d5732ebb9513beba0220444849aa6e5d86b2d01f5e0303dfecdcb7708087f524db0db7ee8a6b631b9a520147522102b085ac037bb3b3ab6de81abf620e42df8d2a51ce4de2905b83bcd514e39f290f2103e795e84d991e34b591f1a7bf2fe7d0489557b3dcf18bd51cd05e4dbebe27bd3f52ae4616b020");

            //var expectedTransaction = Hex.FromString("020000000116d700d0d653aa659a018da8936f9f48307117777037b52455d433d8ccd8face00000000003896d48001c785010000000000160014b2e7cc254f13a72fc66916ff7dc14caac9ef8140f5703d20");

            var transaction = serializationFactory.Deserialize<Transaction>(expectedTransaction);

            var result = _sut.ProcessMessageAsync(new PeerMessage<AcceptChannel>(nodeId, new BoltMessage { Payload = deserializedAcceptChannel }))
                .GetAwaiter().GetResult();

            var candidate = _candidateRepository.ChannelStates.First().Value;

            var trx1 = TransactionHelper.ParseToString(transaction);
            var trx2 = TransactionHelper.ParseToString(candidate.RemoteCommitmentTransaction);

            candidate.RemoteCommitmentTransaction.Should()
                .BeEquivalentTo(transaction);
        }
    }
}