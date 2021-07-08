using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;

namespace Lyn.Protocol.Bolt7
{
   public class ChannelAnnouncementService : IBoltMessageService<ChannelAnnouncement>
   {
      private readonly IMessageValidator<ChannelAnnouncement> _messageValidator;
      private readonly IGossipRepository _gossipRepository;
      private readonly IBoltFeatures _boltFeatures;
      
      public ChannelAnnouncementService(IMessageValidator<ChannelAnnouncement> messageValidator, IGossipRepository gossipRepository, IBoltFeatures boltFeatures)
      {
         _messageValidator = messageValidator;
         _gossipRepository = gossipRepository;
         _boltFeatures = boltFeatures;
      }

      public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ChannelAnnouncement> request)
      {
         var message = request.MessagePayload;
         
         if (!_messageValidator.ValidateMessage(message))
            return new MessageProcessingOutput(); //ignore message

         var existingChannel = await _gossipRepository.GetGossipChannelAsync(message.ShortChannelId);

         if (existingChannel != null)
         {
            if (existingChannel.ChannelAnnouncement.NodeId1 != message.NodeId1 || existingChannel.ChannelAnnouncement.NodeId2 != message.NodeId2)
            {
               await BlacklistNodesAndForgetChannels(message, existingChannel.ChannelAnnouncement);
            }
         }
            
         
         //TODO David add logic to verify P2WSH for bitcoin keys on the blockchain using short channel id

         var gossipChannel = new GossipChannel(request.MessagePayload);
         
         gossipChannel.UnsupportedFeatures = _boltFeatures.ContainsUnknownRequiredFeatures(message.Features);
         
         await _gossipRepository.AddGossipChannelAsync(gossipChannel);

         return new EmptySuccessResponse();
      }

      private async Task BlacklistNodesAndForgetChannels(ChannelAnnouncement message, ChannelAnnouncement existingChannel)
      {
         var nodes = await _gossipRepository.GetNodesAsync(existingChannel.NodeId1, existingChannel.NodeId2,
            message.NodeId1, message.NodeId2);

         var nodeIds = nodes.Select(_ => _.NodeAnnouncement.NodeId).ToArray()
            .Union(new[] {message.NodeId1, message.NodeId2, existingChannel.NodeId1, existingChannel.NodeId2})
            .ToArray();

         await _gossipRepository.AddNodeToBlacklistAsync(nodeIds);

         var channelsToForget = nodes.SelectMany(_ => _.Channels);

         await _gossipRepository.RemoveGossipChannelsAsync(channelsToForget
            .Select(_ => _.ChannelAnnouncement.ShortChannelId).ToArray());
      }
   }
}