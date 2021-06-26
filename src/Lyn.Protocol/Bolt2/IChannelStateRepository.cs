using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2
{
    public interface IChannelStateRepository
    {
        void Create(ChannelState channelState);

        void Update(ChannelState channelState);

        ChannelState? Get(ChannelId channelId);
    }
}