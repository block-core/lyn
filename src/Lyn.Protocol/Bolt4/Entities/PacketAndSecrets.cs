using Lyn.Types.Fundamental;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt4.Entities
{
    public struct PacketAndSecrets
    {
        public OnionRoutingPacket Packet { get; set; }
        public IEnumerable<(byte[], PublicKey)> SharedSecrets { get; set; }
    }
}
