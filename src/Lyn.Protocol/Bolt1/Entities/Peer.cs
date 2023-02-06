using System.Collections.Generic;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1.Entities
{
    public class Peer
    {
        public ulong Id { get; set; }
        
        public PublicKey NodeId { get; set; }

        public Features Features { get; set; }

        public Features GlobalFeatures { get; set; }

        public Features MutuallySupportedFeatures { get; set; }

        public List<UInt256> PaymentChannelIds { get; set; }
        
        public bool SupportsFeature(Features feature)
        {
            return (Features & feature) != 0;
        }
        
        public bool MutuallySupportedFeature(Features feature)
        {
            return (MutuallySupportedFeatures & feature) != 0;
        }
    }
}