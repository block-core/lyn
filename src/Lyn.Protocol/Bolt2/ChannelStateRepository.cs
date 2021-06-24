using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt2
{
    public class ChannelStateRepository : IChannelStateRepository
    {
        public void AddOrUpdate(ChannelState channelState)
        {
            throw new NotImplementedException();
        }

        public ChannelState Get(ChannelId channelId)
        {
            throw new NotImplementedException();
        }
    }
}