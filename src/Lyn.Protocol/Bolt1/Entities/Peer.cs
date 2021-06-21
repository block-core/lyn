using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1.Entities
{
    public class Peer
    {
        public ulong Id { get; set; }
        
        public PublicKey NodeId { get; set; }

        public byte[] Featurs { get; set; }

        public byte[] GlobalFeatures { get; set; }
    }
}