using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Bolt9;
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

      public async Task ProcessMessageAsync(PeerMessage<ChannelAnnouncement> request)
      {
         var message = request.MessagePayload;
         
         if (!_messageValidator.ValidateMessage(message))
            return; //ignore message

         var existingChannel = _gossipRepository.GetGossipChannel(message.ShortChannelId);

         if (existingChannel != null)
         {
            if (existingChannel.ChannelAnnouncement.NodeId1 != message.NodeId1 || existingChannel.ChannelAnnouncement.NodeId2 != message.NodeId2)
            {
               BlacklistNodesAndForgetChannels(message, existingChannel.ChannelAnnouncement);
            }
         }
            
         
         //TODO David add logic to verify P2WSH for bitcoin keys on the blockchain using short channel id

         var gossipChannel = new GossipChannel(request.MessagePayload);
         
         gossipChannel.UnsupportedFeatures = _boltFeatures.ContainsUnknownRequiredFeatures(message.Features);
         
         _gossipRepository.AddGossipChannel(gossipChannel);
      }

      private void BlacklistNodesAndForgetChannels(ChannelAnnouncement message, ChannelAnnouncement existingChannel)
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