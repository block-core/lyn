using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt7
{
   public class ChannelAnnouncementService : IGossipMessageService<ChannelAnnouncement>
   {
      readonly IMessageValidator<ChannelAnnouncement> _messageValidator;
      readonly IGossipRepository _gossipRepository;
      
      public ChannelAnnouncementService(IMessageValidator<ChannelAnnouncement> messageValidator, IGossipRepository gossipRepository)
      {
         _messageValidator = messageValidator;
         _gossipRepository = gossipRepository;
      }

      public MessageProcessingOutput ProcessMessage(ChannelAnnouncement message)
      {
         throw new NotImplementedException();
      }

      public async ValueTask<MessageProcessingOutput> ProcessMessageAsync(ChannelAnnouncement message, CancellationToken cancellation)
      {
         (bool isValid, ErrorMessage? errorMessage) = _messageValidator.ValidateMessage(message);
         if (!isValid)
         {
            if (errorMessage == null)
               throw new ArgumentException(nameof(message));

            return new MessageProcessingOutput {ErrorMessage = errorMessage};
         }

         var existingChannel = _gossipRepository.GetGossipChannel(message.ShortChannelId);

         if (existingChannel != null)
         {
            if (existingChannel.ChannelAnnouncement.NodeId1 != message.NodeId1 || existingChannel.ChannelAnnouncement.NodeId2 != message.NodeId2)
            {
               BlacklistNodesAndForgetChannels(message, existingChannel.ChannelAnnouncement);
            }
         }
            
         
         //TODO David add logic to verify P2WSH for bitcoin keys
         

         return new MessageProcessingOutput{Success = true};
      }

      void BlacklistNodesAndForgetChannels(ChannelAnnouncement message, ChannelAnnouncement existingChannel)
      {
         var nodes = _gossipRepository.GetNodes(existingChannel.NodeId1, existingChannel.NodeId2,
            message.NodeId1, message.NodeId2);

         var nodeIds = nodes.Select(_ => _.NodeAnnouncement.NodeId).ToArray()
            .Union(new[] {message.NodeId1, message.NodeId2, existingChannel.NodeId1, existingChannel.NodeId2})
            .ToArray();

         _gossipRepository.AddNodeToBlacklist(nodeIds);

         var channelsToForget = nodes.SelectMany(_ => _.Channels);

         _gossipRepository.RemoveGossipChannels(channelsToForget
            .Select(_ => _.ChannelAnnouncement.ShortChannelId).ToArray());
      }
   }
}