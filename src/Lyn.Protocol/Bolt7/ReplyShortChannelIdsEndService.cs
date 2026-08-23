using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;

namespace Lyn.Protocol.Bolt7
{
   /// <summary>
   /// Marks the end of a gossip sync exchange triggered by our query_short_channel_ids message.
   /// </summary>
   public class ReplyShortChannelIdsEndService : IBoltMessageService<ReplyShortChannelIdsEnd>
   {
      public Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ReplyShortChannelIdsEnd> request)
      {
         // The peer has finished responding to one of our queries, nothing to act on yet.
         return Task.FromResult<MessageProcessingOutput>(new EmptySuccessResponse());
      }
   }
}
