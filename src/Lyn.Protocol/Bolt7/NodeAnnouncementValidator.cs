using Lyn.Protocol.Common;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;
using NBitcoin.Crypto;

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

        public (bool, ErrorMessage?) ValidateMessage(NodeAnnouncement networkMessage)
        {
            if (!_validationHelper.VerifyPublicKey(networkMessage.NodeId))
                return (false, null);

            var output =
               _serializationFactory.Serialize(networkMessage)[CompressedSignature.LENGTH..];

            byte[]? doubleHash = Hashes.DoubleSHA256RawBytes(output, 0, output.Length);

            if (!_validationHelper.VerifySignature(networkMessage.NodeId, networkMessage.Signature, doubleHash))
                return (false, null);

            //TODO David validate features including addrlen

            return (true, null);
        }
    }
}