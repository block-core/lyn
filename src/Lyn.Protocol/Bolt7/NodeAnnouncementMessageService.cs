using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt7
{
   public class NodeAnnouncementMessageService : IBoltMessageService<NodeAnnouncement>
   {
      readonly IMessageValidator<NodeAnnouncement> _messageValidator;

      readonly IGossipRepository _gossipRepository;

      public NodeAnnouncementMessageService(IMessageValidator<NodeAnnouncement> messageValidator, IGossipRepository gossipRepository)
      {
         _messageValidator = messageValidator;
         _gossipRepository = gossipRepository;
      }

      public Task ProcessMessageAsync(PeerMessage<NodeAnnouncement> request)
      {
         var message = request.Message;

         if (!_messageValidator.ValidateMessage(message))
            throw new ArgumentException(nameof(message)); //Close connection when failed validation

         //TODO need to get exising channel announcement and update node details
         //TODO missing logic here from Bolt 8

         var node = new GossipNode(message);

         _gossipRepository.AddNode(node);
         
         return Task.CompletedTask;
      }
   }
}