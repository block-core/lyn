using Lyn.Protocol.Bolt1.Messages;
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

        public bool SupportsFeature(Features feature)
        {
            return (Features & Features.OptionUpfrontShutdownScript) != 0;
        }
    }
}