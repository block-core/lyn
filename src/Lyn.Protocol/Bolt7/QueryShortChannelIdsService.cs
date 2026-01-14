using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Bolt;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt7
{
    /// <summary>
    /// Handles incoming query_short_channel_ids messages and responds with
    /// channel_announcement, channel_update, and node_announcement messages.
    /// </summary>
    public class QueryShortChannelIdsService : IBoltMessageService<QueryShortChannelIds>
    {
        private readonly ILogger<QueryShortChannelIdsService> _logger;
        private readonly IGossipRepository _gossipRepository;

        public QueryShortChannelIdsService(
            ILogger<QueryShortChannelIdsService> logger,
            IGossipRepository gossipRepository)
        {
            _logger = logger;
            _gossipRepository = gossipRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<QueryShortChannelIds> message)
        {
            var query = message.MessagePayload;

            if (query.ChainHash == null)
            {
                _logger.LogWarning("Received query_short_channel_ids with null chain_hash");
                return new EmptySuccessResponse();
            }

            // Decode the short_channel_ids from the encoded format
            var shortChannelIds = DecodeShortChannelIds(query.EncodedShortIds);

            if (shortChannelIds.Count == 0)
            {
                _logger.LogDebug("Received query_short_channel_ids with no channel IDs");
                return CreateReplyEnd(query.ChainHash, complete: true);
            }

            _logger.LogDebug("Processing query_short_channel_ids for {Count} channels", shortChannelIds.Count);

            var responseMessages = new List<BoltMessage>();

            foreach (var shortChannelId in shortChannelIds)
            {
                var channel = await _gossipRepository.GetGossipChannelAsync(shortChannelId);

                if (channel == null)
                {
                    _logger.LogDebug("Channel {ShortChannelId} not found in gossip repository", shortChannelId);
                    continue;
                }

                // Add channel_announcement
                responseMessages.Add(new BoltMessage { Payload = channel.ChannelAnnouncement });

                // Add channel_update for direction 1 if available
                if (channel.ChannelUpdateNode1 != null)
                {
                    responseMessages.Add(new BoltMessage { Payload = channel.ChannelUpdateNode1 });
                }

                // Add channel_update for direction 2 if available
                if (channel.ChannelUpdateNode2 != null)
                {
                    responseMessages.Add(new BoltMessage { Payload = channel.ChannelUpdateNode2 });
                }
            }

            // Add reply_short_channel_ids_end to indicate completion
            responseMessages.Add(new BoltMessage
            {
                Payload = new ReplyShortChannelIdsEnd
                {
                    ChainHash = query.ChainHash,
                    Complete = 1
                }
            });

            return new SuccessWithOutputResponse(responseMessages.ToArray());
        }

        private List<ShortChannelId> DecodeShortChannelIds(byte[]? encodedData)
        {
            var result = new List<ShortChannelId>();

            if (encodedData == null || encodedData.Length < 1)
                return result;

            // First byte is encoding type: 0 = uncompressed, 1 = zlib compressed
            byte encodingType = encodedData[0];
            var data = encodedData.AsSpan(1);

            if (encodingType == 0)
            {
                // Uncompressed - each short_channel_id is 8 bytes
                for (int i = 0; i + 8 <= data.Length; i += 8)
                {
                    var scidBytes = data.Slice(i, 8).ToArray();
                    result.Add(new ShortChannelId(scidBytes));
                }
            }
            else if (encodingType == 1)
            {
                // Zlib compressed - decompress first then parse
                // TODO: Implement zlib decompression
                _logger.LogWarning("Zlib compressed short_channel_ids not yet supported");
            }
            else
            {
                _logger.LogWarning("Unknown encoding type {EncodingType} for short_channel_ids", encodingType);
            }

            return result;
        }

        private MessageProcessingOutput CreateReplyEnd(Lyn.Types.Bitcoin.UInt256 chainHash, bool complete)
        {
            return new SuccessWithOutputResponse(new BoltMessage
            {
                Payload = new ReplyShortChannelIdsEnd
                {
                    ChainHash = chainHash,
                    Complete = (byte)(complete ? 1 : 0)
                }
            });
        }
    }
}

