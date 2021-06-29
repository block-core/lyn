using Lyn.Protocol.Common;
using Lyn.Types.Bolt.Messages;
using NBitcoin.Crypto;

namespace Lyn.Protocol.Bolt7
{
   public class AnnouncementSignaturesValidator : IMessageValidator<AnnouncementSignatures>
   {
      private readonly ISerializationFactory _serializationFactory;
      private readonly IGossipRepository _repository;
      private readonly IValidationHelper _validationHelper;

      public AnnouncementSignaturesValidator(IGossipRepository repository, ISerializationFactory serializationFactory, IValidationHelper validationHelper)
      {
         _repository = repository;
         _serializationFactory = serializationFactory;
         _validationHelper = validationHelper;
      }

      public bool ValidateMessage(AnnouncementSignatures networkMessage)
      {
         var channel = _repository.GetGossipChannel(networkMessage.ShortChannelId);

         if (channel?.IsChannelWithLocalNode() != true)
            return false;
         
         var channelAnnouncement = _serializationFactory.Serialize(channel.ChannelAnnouncement)[256..]; 

         var hash = Hashes.DoubleSHA256RawBytes(channelAnnouncement, 0, channelAnnouncement.Length);

         if (!_validationHelper.VerifySignature(channel.GetRemoteNodeId(), networkMessage.NodeSignature, hash) ||
             !_validationHelper.VerifySignature(channel.GetRemoteBitcoinAddress(), networkMessage.BitcoinSignature, hash))
            return false;

         return true;
      }
   }
}