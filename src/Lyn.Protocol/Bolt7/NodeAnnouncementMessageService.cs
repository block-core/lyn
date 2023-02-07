using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;

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

      public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<NodeAnnouncement> request)
      {
         var message = request.MessagePayload;

         if (!_messageValidator.ValidateMessage(message))
            throw new ArgumentException(nameof(message)); //Close connection when failed validation

         //TODO need to get exising channel announcement and update node details
         //TODO missing logic here from Bolt 8

         var node = new GossipNode(message);

         await _gossipRepository.AddNodeAsync(node);
         
         return new EmptySuccessResponse();
      }
   }
}