using System;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt1.Entities
{
    public class TrackedPingPong
    {
        public DateTime Received { get; set; }
        
        public PingMessage PingMessage { get; set; }
    }
}