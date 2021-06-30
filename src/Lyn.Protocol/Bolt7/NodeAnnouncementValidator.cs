using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7
{
    public class NodeAnnouncementValidator : IMessageValidator<NodeAnnouncement>
    {
        private readonly IValidationHelper _validationHelper;
        private readonly ISerializationFactory _serializationFactory;

        public NodeAnnouncementValidator(IValidationHelper validationHelper, ISerializationFactory serializationFactory)
        {
            _validationHelper = validationHelper;
            _serializationFactory = serializationFactory;
        }

        public bool ValidateMessage(NodeAnnouncement networkMessage)
        {
            if (!_validationHelper.VerifyPublicKey(networkMessage.NodeId))
                return false;

            var output = _serializationFactory.Serialize(networkMessage)[CompressedSignature.LENGTH..];

            var doubleHash = HashGenerator.DoubleSha256AsUInt256(output)
                .GetBytes()
                .ToArray();
            
            if (!_validationHelper.VerifySignature(networkMessage.NodeId, networkMessage.Signature, doubleHash))
                return false;

            //TODO David validate features including addrlen

            return true;
        }
    }
}