using System;
using System.Text;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Common.Messages
{
    public class MessageProcessingOutput
    {
        public bool Success { get; set; }

        public bool CloseChannel { get; set; }

        public BoltMessage[]? ResponseMessages { get; set; }
    }
}