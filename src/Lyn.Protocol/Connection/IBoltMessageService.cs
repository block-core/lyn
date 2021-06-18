using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Connection
{
    public interface IBoltMessageService<T> where T : BoltMessage
    {
        Task ProcessMessageAsync(PeerMessage<T> message);
    }
}