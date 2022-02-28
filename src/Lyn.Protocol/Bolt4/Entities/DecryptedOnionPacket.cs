using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4.Entities
{
    public class DecryptedOnionPacket
    {

        public byte[] Payload { get; set; }

        public OnionRoutingPacket NextPacket { get; set; }

        public byte[] SharedSecret { get; set; }

        public bool IsLastPacket => (NextPacket?.Hmac == null || NextPacket?.Hmac.Length == 0);

    }
}
