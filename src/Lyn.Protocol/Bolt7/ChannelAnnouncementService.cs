using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt7
{
   public class ChannelAnnouncementService : IBoltMessageService<ChannelAnnouncement>
   {
      private readonly ILogger<ChannelAnnouncementService> _logger;
      private readonly IMessageValidator<ChannelAnnouncement> _messageValidator;
      private readonly IGossipRepository _gossipRepository;
      private readonly IBoltFeatures _boltFeatures;
      private readonly IBlockchainQueryService? _blockchainQueryService;
      private readonly ILightningScripts _lightningScripts;
      
      public ChannelAnnouncementService(
         ILogger<ChannelAnnouncementService> logger,
         IMessageValidator<ChannelAnnouncement> messageValidator, 
         IGossipRepository gossipRepository, 
         IBoltFeatures boltFeatures,
         ILightningScripts lightningScripts,
         IBlockchainQueryService? blockchainQueryService = null)
      {
         _logger = logger;
         _messageValidator = messageValidator;
         _gossipRepository = gossipRepository;
         _boltFeatures = boltFeatures;
         _lightningScripts = lightningScripts;
         _blockchainQueryService = blockchainQueryService;
      }

      public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ChannelAnnouncement> request)
      {
         var message = request.MessagePayload;
         
         if (!_messageValidator.ValidateMessage(message))
         {
            _logger.LogDebug("Channel announcement failed validation, ignoring");
            return new MessageProcessingOutput(); //ignore message
         }

         var existingChannel = await _gossipRepository.GetGossipChannelAsync(message.ShortChannelId);

         if (existingChannel != null)
         {
            if (existingChannel.ChannelAnnouncement.NodeId1 != message.NodeId1 || 
                existingChannel.ChannelAnnouncement.NodeId2 != message.NodeId2)
            {
               _logger.LogWarning("Channel announcement mismatch for {ShortChannelId}, blacklisting nodes",
                  message.ShortChannelId);
               await BlacklistNodesAndForgetChannels(message, existingChannel.ChannelAnnouncement);
            }
            return new EmptySuccessResponse(); // Already have this channel
         }
         
         // Verify P2WSH for bitcoin keys on the blockchain using short_channel_id
         if (_blockchainQueryService != null)
         {
            var isValid = await VerifyChannelFundingOutputAsync(message);
            if (!isValid)
            {
               _logger.LogWarning("Channel announcement failed blockchain verification for {ShortChannelId}",
                  message.ShortChannelId);
               return new MessageProcessingOutput(); // Ignore invalid announcement
            }
         }
         else
         {
            _logger.LogDebug("Blockchain query service not available, skipping P2WSH verification");
         }

         var gossipChannel = new GossipChannel(request.MessagePayload);
         
         gossipChannel.UnsupportedFeatures = _boltFeatures.ContainsUnknownRequiredFeatures(message.Features);
         
         await _gossipRepository.AddGossipChannelAsync(gossipChannel);

         _logger.LogDebug("Added channel announcement for {ShortChannelId}", message.ShortChannelId);

         return new EmptySuccessResponse();
      }

      /// <summary>
      /// Verifies that the channel funding output exists on chain and matches the announced bitcoin keys.
      /// </summary>
      private async Task<bool> VerifyChannelFundingOutputAsync(ChannelAnnouncement message)
      {
         if (_blockchainQueryService == null)
            return true;

         // Generate the expected P2WSH from the bitcoin keys in the announcement
         var fundingRedeemScript = _lightningScripts.FundingRedeemScript(
            message.BitcoinKey1, message.BitcoinKey2);
         
         var p2wshScript = _lightningScripts.ToP2WSH(fundingRedeemScript);

         // Verify the output exists at the location specified by short_channel_id
         return await _blockchainQueryService.VerifyP2WSHExistsAsync(
            message.ShortChannelId, p2wshScript);
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