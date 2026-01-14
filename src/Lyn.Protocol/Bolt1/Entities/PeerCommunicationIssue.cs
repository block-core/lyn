using System;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt1.Entities
{
    public class PeerCommunicationIssue
    {
        public MessageType MessageType { get; set; }

        public UInt256 ChannelId { get; set; }

        public string MessageText { get; set; } = string.Empty;
        
        /// <summary>
        /// Indicates if this is a warning (true) or error (false).
        /// Warnings don't close channels but may indicate problematic peers.
        /// </summary>
        public bool IsWarning { get; set; }
        
        /// <summary>
        /// Timestamp when the issue was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}