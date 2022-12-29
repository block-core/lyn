using System.Linq;
using FluentAssertions;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Wallet;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Crypto;
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
            _features.Object,
            new Mock<IWalletTransactions>().Object);
        }
    }
}