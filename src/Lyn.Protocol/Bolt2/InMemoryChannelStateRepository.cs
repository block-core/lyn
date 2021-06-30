using Lyn.Protocol.Bolt2.Entities;
using Lyn.Types.Bolt;
using System.Collections.Concurrent;

namespace Lyn.Protocol.Bolt2
{
    public class InMemoryChannelStateRepository : IChannelStateRepository
    {
        public ConcurrentDictionary<ChannelId, ChannelState> ChannelStates = new();

        public void Create(ChannelState channelState)
        {
            ChannelStates.TryAdd(channelState.ChannelId, channelState);
        }

        public void Update(ChannelState channelState)
        {
            ChannelStates.TryUpdate(channelState.ChannelId, channelState, channelState);
        }

        public ChannelState? Get(ChannelId channelId)
        {
            if (ChannelStates.TryGetValue(channelId, out var channelState))
                return channelState;

            return null;
        }
    }
}