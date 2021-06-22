using System;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt7
{
   public class NodeAnnouncementMessageService : IGossipMessageService<NodeAnnouncement>
   {
      readonly IMessageValidator<NodeAnnouncement> _messageValidator;

      readonly IGossipRepository _gossipRepository;

      public NodeAnnouncementMessageService(IMessageValidator<NodeAnnouncement> messageValidator, IGossipRepository gossipRepository)
      {
         _messageValidator = messageValidator;
         _gossipRepository = gossipRepository;
      }

      public MessageProcessingOutput ProcessMessage(NodeAnnouncement message)
      {
         throw new NotImplementedException();
      }

      public async ValueTask<MessageProcessingOutput> ProcessMessageAsync(NodeAnnouncement message, CancellationToken cancellation)
      {
         var (isValid, errorMessage) = _messageValidator.ValidateMessage(message);

         if (!isValid)
         {
            if (errorMessage == null)
               throw new ArgumentException(nameof(message));

            return new MessageProcessingOutput {ErrorMessage = errorMessage};
         }

         var node = new GossipNode(message);
         
         _gossipRepository.AddNode(node);
 
         return new MessageProcessingOutput{Success = true};
      }
   }
}