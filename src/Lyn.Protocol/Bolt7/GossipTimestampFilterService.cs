using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt7
{
   public class GossipTimestampFilterService : IBoltMessageService<GossipTimestampFilter>
   {
      readonly IGossipRepository _gossipRepository;
      
      public GossipTimestampFilterService(IGossipRepository gossipRepository)
      {
         _gossipRepository = gossipRepository;
      }

      public async Task ProcessMessageAsync(PeerMessage<GossipTimestampFilter> request)
      {
         var message = request.Message;
         
         if (message.ChainHash == null)
            throw new ArgumentNullException(nameof(ChainHash));

         if (message.NodeId is null)
            throw new InvalidOperationException();
         
         var node = _gossipRepository.GetNode(message.NodeId);

         if (node == null)
            throw new InvalidOperationException("Node not found in gossip repository"); //we should only be getting this message if the feature is enabled in the handshake

         var existingFilter = node.BlockchainTimeFilters
                                 .SingleOrDefault(_ => _.ChainHash.Equals(message.ChainHash))
                              ?? message;

         existingFilter.FirstTimestamp = message.FirstTimestamp;
         existingFilter.TimestampRange = message.TimestampRange;

         _gossipRepository.AddNode(node);
      }
   }
}