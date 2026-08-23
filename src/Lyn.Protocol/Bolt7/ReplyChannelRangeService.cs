using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt7
{
   /// <summary>
   /// Processes a reply_channel_range message by requesting the short channel ids we do not
   /// know about yet from the peer with a query_short_channel_ids message.
   /// </summary>
   public class ReplyChannelRangeService : IBoltMessageService<ReplyChannelRange>
   {
      private readonly IGossipRepository _gossipRepository;

      public ReplyChannelRangeService(IGossipRepository gossipRepository)
      {
         _gossipRepository = gossipRepository;
      }

      public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ReplyChannelRange> request)
      {
         var reply = request.MessagePayload;

         ShortChannelId[] receivedShortIds = EncodedShortIds.Decode(reply.EncodedShortIds);

         var unknownShortIds = new System.Collections.Generic.List<ShortChannelId>();

         foreach (ShortChannelId shortChannelId in receivedShortIds)
         {
            if (await _gossipRepository.GetGossipChannelAsync(shortChannelId) is null)
               unknownShortIds.Add(shortChannelId);
         }

         if (unknownShortIds.Count == 0)
            return new EmptySuccessResponse();

         byte[] encodedUnknown = EncodedShortIds.Encode(unknownShortIds);

         return new SuccessWithOutputResponse(new BoltMessage
         {
            Payload = new QueryShortChannelIds(reply.ChainHash, (ushort)encodedUnknown.Length, encodedUnknown)
         });
      }
   }
}
