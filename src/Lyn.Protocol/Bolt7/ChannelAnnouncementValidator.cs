using System.Linq;
using Lyn.Protocol.Common;
using Lyn.Types;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
using NBitcoin.Crypto;

namespace Lyn.Protocol.Bolt7
{
   public class ChannelAnnouncementValidator : IMessageValidator<ChannelAnnouncement>
   {
      readonly IGossipRepository _gossipRepository;
      private readonly IValidationHelper _validationHelper;
      private readonly ISerializationFactory _serializationFactory;
 
      public ChannelAnnouncementValidator(IGossipRepository gossipRepository, IValidationHelper validationHelper, ISerializationFactory serializationFactory)
      {
         _gossipRepository = gossipRepository;
         _validationHelper = validationHelper;
         _serializationFactory = serializationFactory;
      }

      public (bool, ErrorMessage?) ValidateMessage(ChannelAnnouncement networkMessage)
      {
         if (!_validationHelper.VerifyPublicKey(networkMessage.NodeId1) || 
             !_validationHelper.VerifyPublicKey(networkMessage.NodeId2) ||
             !_validationHelper.VerifyPublicKey(networkMessage.BitcoinKey1) ||
             !_validationHelper.VerifyPublicKey(networkMessage.BitcoinKey2))
            return (false, null);

         var messageByteArrayWithoutSignatures = _serializationFactory.Serialize(networkMessage)
            [(CompressedSignature.LENGTH * 4)..];

         byte[]? doubleHash = Hashes.DoubleSHA256RawBytes(messageByteArrayWithoutSignatures, 
            0, messageByteArrayWithoutSignatures.Length);

         if (!_validationHelper.VerifySignature(networkMessage.NodeId1, networkMessage.NodeSignature1, doubleHash) ||
             !_validationHelper.VerifySignature(networkMessage.NodeId2, networkMessage.NodeSignature2, doubleHash) ||
             !_validationHelper.VerifySignature(networkMessage.BitcoinKey1, networkMessage.BitcoinSignature1, doubleHash) ||
             !_validationHelper.VerifySignature(networkMessage.BitcoinKey2, networkMessage.BitcoinSignature2, doubleHash))
            return (false, null);
         
         // TODO David add features validation
         // (from lightning rfc) if there is an unknown even bit in the features field:
         // MUST NOT attempt to route messages through the channel.


         if (_gossipRepository.IsNodeInBlacklistedList(networkMessage.NodeId1) ||
             _gossipRepository.IsNodeInBlacklistedList(networkMessage.NodeId2))
            return (false, null);
         
         if (!ChainHashes.SupportedChainHashes.Values.Contains(networkMessage.ChainHash))
            return (false, null);
         
         return (true, null);
      }
   }
}