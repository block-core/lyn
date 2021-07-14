using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public interface IChannelCandidateRepository
    {
        Task CreateAsync(ChannelCandidate channelCandidate);

        Task UpdateAsync(ChannelCandidate channelCandidate);

        Task<ChannelCandidate?> GetAsync(ChannelId channelId);
    }
}