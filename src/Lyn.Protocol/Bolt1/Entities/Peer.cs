using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1.Entities
{
    public class Peer
    {
        public ulong Id { get; set; }
        
        public PublicKey NodeId { get; set; }

        public Features Featurs { get; set; }

        public Features GlobalFeatures { get; set; }
    }
}