using Lyn.Protocol.Bolt7;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Serialization;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;
using Moq;
using NBitcoin.Crypto;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt7
{
    public class ChannelAnnouncementValidatorTests : RandomGossipMessages
    {
        private ChannelAnnouncementValidator _sut;

        private Mock<IGossipRepository> _gossipRepository;
        private Mock<ISerializationFactory> _serializationFactory;
        private Mock<IValidationHelper> _validationHelper;

        public ChannelAnnouncementValidatorTests()
        {
            _gossipRepository = new Mock<IGossipRepository>();
            _serializationFactory = new Mock<ISerializationFactory>();
            _validationHelper = new Mock<IValidationHelper>();

            _sut = new ChannelAnnouncementValidator(_gossipRepository.Object, _validationHelper.Object,
                _serializationFactory.Object);
        }

        [Fact]
        public void WhenNode1PublicKeyIsInvalidReturnsFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            _validationHelper.Verify(_ => _.VerifyPublicKey(channelAnnouncement.NodeId2), Times.Never);

            _validationHelper.Setup(_ => _.VerifyPublicKey(channelAnnouncement.NodeId1))
                .Returns(false)
                .Verifiable();

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenNode2PublicKeyIsInvalidReturnsFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithInvalidPublicKey(channelAnnouncement.NodeId2);

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenBitcoinKey1IsPublicKeyIsInvalidReturnsFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithInvalidPublicKey(channelAnnouncement.BitcoinKey1);

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenBitcoinKey2IsPublicKeyIsInvalidReturnsFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithInvalidPublicKey(channelAnnouncement.BitcoinKey2);

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenNodeSignature1IsInvalidReturnFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithAllPublicKeysValid();

            var serializedMessage = WithSerializedChannelAnnouncement(channelAnnouncement);

            var doubleHash = Hashes.DoubleSHA256RawBytes(serializedMessage, 0, serializedMessage.Length);

            _validationHelper.Verify(_ => _.VerifySignature(It.IsAny<PublicKey>(),
                    It.IsAny<CompressedSignature>(), It.IsAny<UInt256>()), Times.Never);

            _validationHelper.Setup(_ => _.VerifySignature(channelAnnouncement.NodeId1, channelAnnouncement.NodeSignature1,
                   new UInt256(doubleHash)))
                .Returns(false)
                .Verifiable();

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenNodeSignature2IsInvalidReturnFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithAllPublicKeysValid();

            var serializedMessage = WithSerializedChannelAnnouncement(channelAnnouncement);

            WithSignatureThatFailedValidationForNodeId(serializedMessage, channelAnnouncement.NodeId2, channelAnnouncement.NodeSignature2);

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenBitcoinSignature1IsInvalidReturnFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithAllPublicKeysValid();

            var serializedMessage = WithSerializedChannelAnnouncement(channelAnnouncement);

            WithSignatureThatFailedValidationForNodeId(serializedMessage, channelAnnouncement.BitcoinKey1, channelAnnouncement.BitcoinSignature1);

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenBitcoinSignature2IsInvalidReturnFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithAllPublicKeysValid();

            var serializedMessage = WithSerializedChannelAnnouncement(channelAnnouncement);

            WithSignatureThatFailedValidationForNodeId(serializedMessage, channelAnnouncement.BitcoinKey2, channelAnnouncement.BitcoinSignature2);

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenNode1IsBlacklistedReturnsFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithAllPublicKeysValid();
            var serializedMessage = WithSerializedChannelAnnouncement(channelAnnouncement);
            WithAllSignaturesValid(serializedMessage);

            _gossipRepository.Setup(_ => _.IsNodeInBlacklistedList(channelAnnouncement.NodeId1))
                .Returns(true)
                .Verifiable();

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenNode2IsBlacklistedReturnsFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithAllPublicKeysValid();
            var serializedMessage = WithSerializedChannelAnnouncement(channelAnnouncement);
            WithAllSignaturesValid(serializedMessage);

            _gossipRepository.Setup(_ => _.IsNodeInBlacklistedList(channelAnnouncement.NodeId2))
                .Returns(true)
                .Verifiable();

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenTheMessageIsValidButTheChainIsNotSupportedReturnFalse()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            WithAllPublicKeysValid();
            var serializedMessage = WithSerializedChannelAnnouncement(channelAnnouncement);
            WithAllSignaturesValid(serializedMessage);

            _gossipRepository.Setup(_ => _.IsNodeInBlacklistedList(It.IsAny<PublicKey>()))
                .Returns(false)
                .Verifiable();

            var result = _sut.ValidateMessage(channelAnnouncement);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }

        [Fact]
        public void WhenTheMessageIsValidAndTheChainIsSupportedReturnTrue()
        {
            var channelAnnouncement = NewChannelAnnouncement();

            channelAnnouncement.ChainHash = ChainHashes.Bitcoin;

            WithAllPublicKeysValid();
            var serializedMessage = WithSerializedChannelAnnouncement(channelAnnouncement);
            WithAllSignaturesValid(serializedMessage);

            _gossipRepository.Setup(_ => _.IsNodeInBlacklistedList(It.IsAny<PublicKey>()))
                .Returns(false)
                .Verifiable();

            var result = _sut.ValidateMessage(channelAnnouncement);

            Assert.True(result);

            _validationHelper.VerifyAll();
        }

        private void WithSignatureThatFailedValidationForNodeId(byte[] serializedMessage,
            PublicKey nodeId, CompressedSignature signature)
        {
            WithAllSignaturesValid(serializedMessage);

            var doubleHash = Hashes.DoubleSHA256RawBytes(serializedMessage, 0, serializedMessage.Length);

            _validationHelper.Setup(_ => _.VerifySignature(nodeId, signature, new UInt256(doubleHash)))
                .Returns(false)
                .Verifiable();
        }

        private void WithAllSignaturesValid(byte[] serializedMessage)
        {
            var doubleHash = Hashes.DoubleSHA256RawBytes(serializedMessage, 0, serializedMessage.Length);

            _validationHelper.Setup(_ => _.VerifySignature(It.IsAny<PublicKey>(),
                    It.IsAny<CompressedSignature>(), new UInt256(doubleHash)))
                .Returns(true)
                .Verifiable();
        }

        private byte[] WithSerializedChannelAnnouncement(ChannelAnnouncement channelAnnouncement)
        {
            var bytes = RandomMessages.GetRandomByteArray(256 + 174);

            _serializationFactory.Setup(_ => _.Serialize<ChannelAnnouncement>(channelAnnouncement, null))
                .Returns(bytes)
                .Verifiable();

            return bytes[256..]; //return serialization without the signatures
        }

        private void WithInvalidPublicKey(PublicKey publicKey)
        {
            WithAllPublicKeysValid();

            _validationHelper.Setup(_ => _.VerifyPublicKey(publicKey))
                .Returns(false)
                .Verifiable();
        }

        private void WithAllPublicKeysValid()
        {
            _validationHelper.Setup(_ => _.VerifyPublicKey(It.IsAny<PublicKey>()))
                .Returns(true);
        }

        private void ThanTheValidationFailedWithNoErrorMessage(bool result)
        {
            Assert.False(result);
            
            _validationHelper.VerifyAll();
        }
    }
}