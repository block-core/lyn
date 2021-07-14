using System.Collections.Concurrent;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class InMemoryChannelCandidateRepository : IChannelCandidateRepository
    {
        public ConcurrentDictionary<ChannelId, ChannelCandidate> ChannelStates = new();

        public Task CreateAsync(ChannelCandidate channelCandidate)
        {
            ChannelStates.TryAdd(channelCandidate.ChannelId, channelCandidate);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChannelCandidate channelCandidate)
        {
            ChannelStates.TryUpdate(channelCandidate.ChannelId, channelCandidate, channelCandidate);

            return Task.CompletedTask;
        }

        public Task<ChannelCandidate?> GetAsync(ChannelId channelId)
        {
            if (ChannelStates.TryGetValue(channelId, out var channelState))
                return Task.FromResult(channelState)!;

            return Task.FromResult(default(ChannelCandidate?));
        }
    }
}