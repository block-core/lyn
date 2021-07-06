using System.Linq;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Hashing;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7
{
    public class ChannelAnnouncementValidator : IMessageValidator<ChannelAnnouncement>
    {
        private readonly IGossipRepository _gossipRepository;
        private readonly IValidationHelper _validationHelper;
        private readonly ISerializationFactory _serializationFactory;

        public ChannelAnnouncementValidator(IGossipRepository gossipRepository,IValidationHelper validationHelper, ISerializationFactory serializationFactory)
        {
            _gossipRepository = gossipRepository;
            _validationHelper = validationHelper;
            _serializationFactory = serializationFactory;
        }

        public bool ValidateMessage(ChannelAnnouncement networkMessage)
        {
            if (!_validationHelper.VerifyPublicKey(networkMessage.NodeId1) ||
                !_validationHelper.VerifyPublicKey(networkMessage.NodeId2) ||
                !_validationHelper.VerifyPublicKey(networkMessage.BitcoinKey1) ||
                !_validationHelper.VerifyPublicKey(networkMessage.BitcoinKey2))
                return false;

            var messageByteArrayWithoutSignatures = _serializationFactory.Serialize(networkMessage)
               [(CompressedSignature.LENGTH * 4)..];

             var doubleHash = HashGenerator.DoubleSha256AsUInt256(messageByteArrayWithoutSignatures);
            
            if (!_validationHelper.VerifySignature(networkMessage.NodeId1, networkMessage.NodeSignature1, doubleHash) ||
                !_validationHelper.VerifySignature(networkMessage.NodeId2, networkMessage.NodeSignature2, doubleHash) ||
                !_validationHelper.VerifySignature(networkMessage.BitcoinKey1, networkMessage.BitcoinSignature1, doubleHash) ||
                !_validationHelper.VerifySignature(networkMessage.BitcoinKey2, networkMessage.BitcoinSignature2, doubleHash))
                return false;

            if (_gossipRepository.IsNodeInBlacklistedList(networkMessage.NodeId1) ||
                _gossipRepository.IsNodeInBlacklistedList(networkMessage.NodeId2))
                return false;
            
            if (!ChainHashes.SupportedChainHashes.Values.Contains(networkMessage.ChainHash))
                return false;

            return true;
        }
    }
}