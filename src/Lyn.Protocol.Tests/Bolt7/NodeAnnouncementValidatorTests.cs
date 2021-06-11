using Lyn.Protocol.Bolt7;
using Lyn.Protocol.Common;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;
using Moq;
using NBitcoin.Crypto;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt7
{
    public class NodeAnnouncementValidatorTests : RandomGossipMessages
    {
        private NodeAnnouncementValidator _sut;

        private Mock<IGossipRepository> _gossipRepository;
        private Mock<ISerializationFactory> _serializationFactory;
        private Mock<IValidationHelper> _validationHelper;
        
        public NodeAnnouncementValidatorTests()
        {
            _gossipRepository = new Mock<IGossipRepository>();
            _serializationFactory = new Mock<ISerializationFactory>();
            _validationHelper = new Mock<IValidationHelper>();

            _sut = new NodeAnnouncementValidator(_validationHelper.Object,_serializationFactory.Object);
        }
        
        private void ThanTheValidationFailedWithNoErrorMessage((bool, ErrorMessage?) result)
        {
            var (isValid, errorMessage) = result;
            Assert.False(isValid);
            Assert.Null(errorMessage);
            
            _validationHelper.VerifyAll();
        }
        
        private void WithAllPublicKeysValid()
        {
            _validationHelper.Setup(_ => _.VerifyPublicKey(It.IsAny<PublicKey>()))
                .Returns(true);
        }
        
        private byte[] WithSerializedNodeAnnouncement(NodeAnnouncement nodeAnnouncement)
        {
            var bytes = RandomMessages.GetRandomByteArray(256);

            _serializationFactory.Setup(_ => _.Serialize(nodeAnnouncement))
                .Returns(bytes)
                .Verifiable();

            return bytes[CompressedSignature.LENGTH..]; 
        }
        
        [Fact]
        public void WhenTheNodeSignatureIsNotValidReturnsFalse()
        {
            var message = NewNodeAnnouncement();
            
            _validationHelper.Setup(_ => _.VerifyPublicKey(message.NodeId))
                .Returns(false);
            
            var result = _sut.ValidateMessage(message);

            ThanTheValidationFailedWithNoErrorMessage(result);
        }
        
        [Fact]
        public void WhenNodeSignature1IsInvalidReturnFalse()
        {
            var message = NewNodeAnnouncement();
            
            WithAllPublicKeysValid();

            var serializedMessage = WithSerializedNodeAnnouncement(message);

            var doubleHash = Hashes.DoubleSHA256RawBytes(serializedMessage, 0, serializedMessage.Length);
            

            _validationHelper.Setup(_ => _.VerifySignature(message.NodeId,message.Signature,
                    doubleHash))
                .Returns(false)
                .Verifiable();
            
            var result = _sut.ValidateMessage(message);
            
            ThanTheValidationFailedWithNoErrorMessage(result);
        }
        
        [Fact]
        public void WhenTheMessageIsValidAndTheChainIsSupportedReturnTrue()
        {
            var message = NewNodeAnnouncement();
            
            WithAllPublicKeysValid();
            
            var serializedMessage = WithSerializedNodeAnnouncement(message);

            var doubleHash = Hashes.DoubleSHA256RawBytes(serializedMessage, 0, serializedMessage.Length);

            _validationHelper.Setup(_ => _.VerifySignature(message.NodeId,message.Signature,
                    doubleHash))
                .Returns(true)
                .Verifiable();
            
            var result = _sut.ValidateMessage(message);
            
            Assert.True(result.Item1);
            Assert.Null(result.Item2);
            
            _validationHelper.VerifyAll();
        }
    }
}