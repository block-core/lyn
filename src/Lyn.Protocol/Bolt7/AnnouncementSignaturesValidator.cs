using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Hashing;
using Lyn.Protocol.Common.Messages;

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
         var channel = _repository.GetGossipChannelAsync(networkMessage.ShortChannelId)
            .GetAwaiter()
            .GetResult();

         if (channel?.IsChannelWithLocalNode() != true)
            return false;
         
         var channelAnnouncement = _serializationFactory.Serialize(channel.ChannelAnnouncement)[256..]; 

         var doubleHash = HashGenerator.DoubleSha256AsUInt256(channelAnnouncement);
         
         if (!_validationHelper.VerifySignature(channel.GetRemoteNodeId(), networkMessage.NodeSignature, doubleHash) ||
             !_validationHelper.VerifySignature(channel.GetRemoteBitcoinAddress(), networkMessage.BitcoinSignature, doubleHash))
            return false;

         return true;
      }
   }
}