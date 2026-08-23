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
   /// Responds to a query_channel_range message with a reply_channel_range
   /// message containing the short channel ids we know about in the requested block range.
   /// </summary>
   public class QueryChannelRangeService : IBoltMessageService<QueryChannelRange>
   {
      private readonly IGossipRepository _gossipRepository;

      public QueryChannelRangeService(IGossipRepository gossipRepository)
      {
         _gossipRepository = gossipRepository;
      }

      public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<QueryChannelRange> request)
      {
         var query = request.MessagePayload;

         if (query.NumberOfBlocks == 0)
            throw new ArgumentException("number_of_blocks must be greater than 0", nameof(query.NumberOfBlocks));

         var channels = await _gossipRepository.GetGossipChannelsAsync();

         ulong lastBlockInQuery = (ulong)query.FirstBlocknum + query.NumberOfBlocks;

         ShortChannelId[] shortChannelIdsInRange = channels
            .Where(c =>
            {
               ulong blockHeight = (uint)c.ChannelAnnouncement.ShortChannelId.BlockHeight;
               return blockHeight >= query.FirstBlocknum && blockHeight < lastBlockInQuery;
            })
            .Select(c => c.ChannelAnnouncement.ShortChannelId)
            .OrderBy(s => s.ToUInt64())
            .ToArray();

         byte[] encodedShortIds = EncodedShortIds.Encode(shortChannelIdsInRange);

         // The reply must be split if the encoded ids exceed the maximum length, truncate
         // the range to what fits and flag the reply as incomplete in that case.
         bool complete = encodedShortIds.Length <= EncodedShortIds.MAX_ENCODED_LENGTH;

         var reply = new ReplyChannelRange(
            query.ChainHash,
            query.FirstBlocknum,
            query.NumberOfBlocks,
            complete,
            complete ? encodedShortIds : Array.Empty<byte>());

         return new SuccessWithOutputResponse(new BoltMessage { Payload = reply });
      }
   }
}
