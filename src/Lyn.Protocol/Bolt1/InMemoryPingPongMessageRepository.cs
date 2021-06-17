using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt1
{
    public class InMemoryPingPongMessageRepository : IPingPongMessageRepository
    {
        private readonly ConcurrentDictionary<ushort, TrackedPingPong> _dictionary = new ();
        
        public ValueTask AddPingMessageAsync(DateTime dateTimeGenerated, PingMessage pingMessage)
        {
            _dictionary.TryAdd(pingMessage.BytesLen, new TrackedPingPong
            {
                Received = dateTimeGenerated,
                PingMessage = pingMessage
            });

            return new ValueTask();
        }

        public ValueTask<bool> PendingPingWithIdExistsAsync(ushort pingId)
        {
            var result = _dictionary.ContainsKey(pingId) &&
                         _dictionary[pingId].PongReceived;

            return new ValueTask<bool>(result);
        }

        public ValueTask<TrackedPingPong?> GetPingMessageAsync(ushort pingId)
        {
            var result = _dictionary.ContainsKey(pingId) ? _dictionary[pingId] : (TrackedPingPong?)null;

            return new ValueTask<TrackedPingPong?>(result);
        }

        public ValueTask<bool> MarkPongReplyForPingAsync(ushort pingId)
        {
            if (!_dictionary.ContainsKey(pingId))
                return new ValueTask<bool>(false);

            _dictionary[pingId].PongReceived = true;

            return new ValueTask<bool>(true);
        }
    }
}