using Lyn.Protocol.Bolt7.Messages;

namespace Lyn.Protocol.Bolt7.Entities
{
   public class GossipNode
   {
      public GossipNode(NodeAnnouncement nodeAnnouncement)
      {
         NodeAnnouncement = nodeAnnouncement;
      }

      public NodeAnnouncement NodeAnnouncement { get; set; }

      public GossipTimestampFilter[] BlockchainTimeFilters { get; set; }
      
      public GossipChannel[] Channels { get; set; }
   }
}