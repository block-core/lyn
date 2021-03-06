using System;
using Lyn.Protocol.Bolt1.Messages;

namespace Lyn.Protocol.Bolt1.Entities
{
    public class TrackedPingPong
    {
        public DateTime Created { get; set; }
        
        public PingMessage PingMessage { get; set; }

        public bool PongReceived { get; set; }
    }
}