using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt7
{
    public class AnnouncementSignaturesValidatorTests : RandomGossipMessages
    {
        private AnnouncementSignaturesValidator _sut;

        private Mock<IGossipRepository> _gossipRepository;
        private Mock<ISerializationFactory> _serializationFactory;
        private Mock<IValidationHelper> _validationHelper;

        public AnnouncementSignaturesValidatorTests()
        {
            _gossipRepository = new Mock<IGossipRepository>();
            _serializationFactory = new Mock<ISerializationFactory>();
            _validationHelper = new Mock<IValidationHelper>();

            _sut = new AnnouncementSignaturesValidator(_gossipRepository.Object,
                _serializationFactory.Object, _validationHelper.Object);
        }

        private Span<byte> WithSerializedChannelAnnouncement(ChannelAnnouncement channelAnnouncement)
        {
            var bytes = RandomMessages.GetRandomByteArray(256 + 174);

            _serializationFactory.Setup(_ => _.Serialize<ChannelAnnouncement>(channelAnnouncement, null))
                .Returns(bytes)
                .Verifiable();

            return bytes;
        }

        [Fact]
        public void WhenChannelNotFoundReturnsFalse()
        {
            var result = _sut.ValidateMessage(NewAnnouncementSignatures());

            ThanTheMessageFailedWithNoError(result);
        }

        [Fact]
        public void WhenChannelFoundButNotForLocalNodeReturnsFalse()
        {
            var message = NewAnnouncementSignatures();

            _gossipRepository.Setup(_ => _.GetGossipChannelAsync(message.ShortChannelId))
                .Returns(Task.FromResult(new GossipChannel(NewChannelAnnouncement())));

            var result = _sut.ValidateMessage(message);

            ThanTheMessageFailedWithNoError(result);
        }

        [Fact]
        public void WhenTheNodeSignatureIsNotValidReturnsFalse()
        {
            var message = NewAnnouncementSignatures();
            var gossipChannel = new GossipChannel(NewChannelAnnouncement(), GossipChannel.LocalNode.Node2);

            _gossipRepository.Setup(_ => _.GetGossipChannelAsync(message.ShortChannelId))
                .Returns(Task.FromResult<GossipChannel?>(gossipChannel));

            WithSerializedChannelAnnouncement(gossipChannel.ChannelAnnouncement);

            _validationHelper.SetupSequence(_ => _.VerifySignature(gossipChannel.GetRemoteNodeId(),
                    message.NodeSignature,
                    It.IsAny<UInt256>()))
                .Returns(false);

            var result = _sut.ValidateMessage(message);

            ThanTheMessageFailedWithNoError(result);
        }

        [Fact]
        public void WhenTheBitcoinAddressSignatureIsNotValidReturnsFalse()
        {
            var message = NewAnnouncementSignatures();
            var gossipChannel = new GossipChannel(NewChannelAnnouncement(), GossipChannel.LocalNode.Node1);

            _gossipRepository.Setup(_ => _.GetGossipChannelAsync(message.ShortChannelId))
                .Returns(Task.FromResult<GossipChannel?>(gossipChannel));

            WithSerializedChannelAnnouncement(gossipChannel.ChannelAnnouncement);

            _validationHelper.Setup(_ => _.VerifySignature(gossipChannel.GetRemoteBitcoinAddress(),
                    message.BitcoinSignature,
                    It.IsAny<UInt256>()))
                .Returns(false);

            var result = _sut.ValidateMessage(message);

            ThanTheMessageFailedWithNoError(result);
        }

        [Fact]
        public void ReturnsTrueIfMessageIsValid()
        {
            var message = NewAnnouncementSignatures();
            var gossipChannel = new GossipChannel(NewChannelAnnouncement(), GossipChannel.LocalNode.Node1);

            _gossipRepository.Setup(_ => _.GetGossipChannelAsync(message.ShortChannelId))
                .Returns(Task.FromResult<GossipChannel?>(gossipChannel));

            WithSerializedChannelAnnouncement(gossipChannel.ChannelAnnouncement);

            _validationHelper.Setup(_ => _.VerifySignature(gossipChannel.GetRemoteNodeId(),
                    message.NodeSignature,
                    It.IsAny<UInt256>()))
                .Returns(true);

            _validationHelper.Setup(_ => _.VerifySignature(gossipChannel.GetRemoteBitcoinAddress(),
                    message.BitcoinSignature,
                    It.IsAny<UInt256>()))
                .Returns(true);

            var result = _sut.ValidateMessage(message);

            Assert.True(result);
        }

        private static void ThanTheMessageFailedWithNoError(bool result)
        {
            Assert.False(result);
        }
    }
}