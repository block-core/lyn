using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt7
{
   /// <summary>
   /// Responds to a query_short_channel_ids message by sending the channel announcements we
   /// have for the requested short channel ids, ending with reply_short_channel_ids_end.
   /// </summary>
   public class QueryShortChannelIdsService : IBoltMessageService<QueryShortChannelIds>
   {
      private readonly IGossipRepository _gossipRepository;

      public QueryShortChannelIdsService(IGossipRepository gossipRepository)
      {
         _gossipRepository = gossipRepository;
      }

      public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<QueryShortChannelIds> request)
      {
         var query = request.MessagePayload;

         ShortChannelId[] requestedShortIds = EncodedShortIds.Decode(query.EncodedShortIds);

         var responses = new System.Collections.Generic.List<BoltMessage>();

         bool complete = true;

         foreach (ShortChannelId shortChannelId in requestedShortIds)
         {
            var channel = await _gossipRepository.GetGossipChannelAsync(shortChannelId);

            if (channel is null)
            {
               complete = false;

               continue;
            }

            responses.Add(new BoltMessage { Payload = channel.ChannelAnnouncement });
         }

         responses.Add(new BoltMessage
         {
            Payload = new ReplyShortChannelIdsEnd(query.ChainHash, complete)
         });

         return new SuccessWithOutputResponse(responses.ToArray());
      }
   }
}
