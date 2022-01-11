﻿using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public interface IChannelCandidateRepository
    {
        Task CreateAsync(ChannelCandidate channelCandidate);

        Task UpdateAsync(ChannelCandidate channelCandidate);

        Task UpdateChannelIdAsync(UInt256 tempChannelId, UInt256 channelId);

        Task<ChannelCandidate?> GetAsync(UInt256 channelId);
    }
}