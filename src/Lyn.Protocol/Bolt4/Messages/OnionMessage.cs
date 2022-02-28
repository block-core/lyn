using Lyn.Protocol.Bolt4.Entities;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Fundamental;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4.Messages
{
    public class OnionMessage : MessagePayload
    {
        public override MessageType MessageType => MessageType.OnionMessage;

        public PublicKey BlindingKey { get; set; }

        public OnionRoutingPacket OnionPacket { get; set; }

        public OnionMessage()
        {
            OnionPacket = new OnionRoutingPacket();
        }

    }
}
