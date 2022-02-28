using Lyn.Types.Fundamental;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4.Entities
{
    public class OnionRoutingPacket
    {

        public byte Version { get; set; }

        public PublicKey EphemeralKey { get; set; }

        public byte[] PayloadData { get; set; } 

        public byte[] Hmac { get; set; }

    }
}
