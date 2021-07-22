using Lyn.Protocol.Bolt7.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7.Entities
{
   public class GossipNode
   {
      public GossipNode(PublicKey nodeId)
      {
         Id = nodeId;
      }
      
      public GossipNode(NodeAnnouncement nodeAnnouncement)
      {
         NodeAnnouncement = nodeAnnouncement;
      }

      public PublicKey Id { get; set; }

      public NodeAnnouncement? NodeAnnouncement { get; set; }

      public GossipTimestampFilter[] BlockchainTimeFilters { get; set; } = new GossipTimestampFilter[0];
      
      public GossipChannel[] Channels { get; set; }
   }
}