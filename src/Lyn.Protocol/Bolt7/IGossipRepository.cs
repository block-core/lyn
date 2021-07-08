using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7
{
   public interface IGossipRepository
   {
      Task<GossipNode> AddNodeAsync(GossipNode node);

      Task<GossipNode?> GetNodeAsync(PublicKey nodeId);
      Task<GossipNode[]> GetNodesAsync(params PublicKey[] keys);
      
      Task<GossipChannel> AddGossipChannelAsync(GossipChannel channel);

      Task<GossipChannel?> GetGossipChannelAsync(ShortChannelId shortChannelId);

      Task RemoveGossipChannelsAsync(params ShortChannelId[] channelIds);
      
      bool IsNodeInBlacklistedList(PublicKey nodeId);

      Task AddNodeToBlacklistAsync(params PublicKey[] publicKeys);
   }
}