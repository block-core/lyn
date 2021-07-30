using System.Collections.Concurrent;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class InMemoryChannelCandidateRepository : IChannelCandidateRepository
    {
        public ConcurrentDictionary<UInt256, ChannelCandidate> ChannelStates = new();

        public Task CreateAsync(ChannelCandidate channelCandidate)
        {
            ChannelStates.TryAdd((UInt256)channelCandidate.ChannelId, channelCandidate);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChannelCandidate channelCandidate)
        {
            ChannelStates.TryUpdate((UInt256)channelCandidate.ChannelId, channelCandidate, channelCandidate);

            return Task.CompletedTask;
        }

        public Task<ChannelCandidate?> GetAsync(UInt256 channelId)
        {
            if (ChannelStates.TryGetValue((UInt256)channelId, out var channelState))
                return Task.FromResult(channelState)!;

            return Task.FromResult(default(ChannelCandidate?));
        }
    }
}