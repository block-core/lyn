using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt7
{
    /// <summary>
    /// Handles incoming query_channel_range messages and responds with
    /// reply_channel_range containing short_channel_ids in the requested block range.
    /// </summary>
    public class QueryChannelRangeService : IBoltMessageService<QueryChannelRange>
    {
        private readonly ILogger<QueryChannelRangeService> _logger;
        private readonly IGossipRepository _gossipRepository;

        // Maximum number of short_channel_ids to include in a single reply
        private const int MaxChannelsPerReply = 8000;

        public QueryChannelRangeService(
            ILogger<QueryChannelRangeService> logger,
            IGossipRepository gossipRepository)
        {
            _logger = logger;
            _gossipRepository = gossipRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<QueryChannelRange> message)
        {
            var query = message.MessagePayload;

            if (query.ChainHash == null)
            {
                _logger.LogWarning("Received query_channel_range with null chain_hash");
                return new EmptySuccessResponse();
            }

            _logger.LogDebug("Processing query_channel_range: first_blocknum={FirstBlock}, number_of_blocks={NumBlocks}",
                query.FirstBlocknum, query.NumberOfBlocks);

            // Get all channels in the requested range from the gossip repository
            var channels = await _gossipRepository.GetChannelsInBlockRangeAsync(
                query.FirstBlocknum,
                query.FirstBlocknum + query.NumberOfBlocks - 1);

            var shortChannelIds = channels
                .Select(c => c.ChannelAnnouncement.ShortChannelId)
                .OrderBy(s => s) // Sort by short_channel_id
                .ToList();

            _logger.LogDebug("Found {Count} channels in range", shortChannelIds.Count);

            // If no channels, return a single reply with complete=1
            if (shortChannelIds.Count == 0)
            {
                return new SuccessWithOutputResponse(new BoltMessage
                {
                    Payload = new ReplyChannelRange
                    {
                        ChainHash = query.ChainHash,
                        FirstBlocknum = query.FirstBlocknum,
                        NumberOfBlocks = query.NumberOfBlocks,
                        Complete = 1,
                        Len = 1,
                        EncodedShortIds = new byte[] { 0 } // Encoding type 0 (uncompressed), no channels
                    }
                });
            }

            // Split into multiple replies if needed
            var responseMessages = new List<BoltMessage>();
            var batches = shortChannelIds
                .Select((scid, index) => new { scid, index })
                .GroupBy(x => x.index / MaxChannelsPerReply)
                .Select(g => g.Select(x => x.scid).ToList())
                .ToList();

            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                var isLast = (i == batches.Count - 1);
                var encodedIds = EncodeShortChannelIds(batch);

                responseMessages.Add(new BoltMessage
                {
                    Payload = new ReplyChannelRange
                    {
                        ChainHash = query.ChainHash,
                        FirstBlocknum = query.FirstBlocknum,
                        NumberOfBlocks = query.NumberOfBlocks,
                        Complete = (byte)(isLast ? 1 : 0),
                        Len = (ushort)encodedIds.Length,
                        EncodedShortIds = encodedIds
                    }
                });
            }

            return new SuccessWithOutputResponse(responseMessages.ToArray());
        }

        private byte[] EncodeShortChannelIds(List<ShortChannelId> shortChannelIds)
        {
            // Use uncompressed encoding (type 0)
            var result = new byte[1 + (shortChannelIds.Count * 8)];
            result[0] = 0; // Encoding type 0 = uncompressed

            for (int i = 0; i < shortChannelIds.Count; i++)
            {
                var bytes = shortChannelIds[i].GetBytes();
                Buffer.BlockCopy(bytes, 0, result, 1 + (i * 8), 8);
            }

            return result;
        }
    }
}

