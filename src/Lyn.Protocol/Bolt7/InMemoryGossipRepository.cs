using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7
{
   public class InMemoryGossipRepository : IGossipRepository
   {
      readonly ConcurrentDictionary<PublicKey, GossipNode> _nodes;
      readonly ConcurrentBag<PublicKey> _blacklistedNodeDictionary;
      readonly ConcurrentDictionary<byte[], GossipChannel> _channels;
      
      public InMemoryGossipRepository()
      {
         _blacklistedNodeDictionary = new ConcurrentBag<PublicKey>();
         _nodes = new ConcurrentDictionary<PublicKey, GossipNode>();
         _channels = new ConcurrentDictionary<byte[], GossipChannel>();
      }

      public Task<GossipNode> AddNodeAsync(GossipNode node)
      {
         _nodes.AddOrUpdate(node.Id, node,
            (key, gossipNode) => !key.Equals(node.NodeAnnouncement?.NodeId ?? node.Id)
               ? throw new ArgumentException(nameof(node))
               : node);
         
         return Task.FromResult(node);
      }

      public Task<GossipNode> UpdateNodeAsync(GossipNode node)
      {
         if (!_nodes.TryUpdate(node.Id, node, _nodes[node.Id]))
            throw new InvalidOperationException();
         
         return Task.FromResult(node);
      }

      public Task<GossipNode?> GetNodeAsync(PublicKey nodeId)
      {
         _nodes.TryGetValue(nodeId, out GossipNode? node);

         return Task.FromResult(node);
      }

      public Task<GossipNode[]> GetNodesAsync(params PublicKey[] keys)
      {
         var nodes = _nodes.Where(_ => keys.Contains(_.Key))
            .Select(_ => _.Value)
            .ToArray();
         
         return Task.FromResult(nodes);
      }

      public Task<GossipChannel> AddGossipChannelAsync(GossipChannel channel)
      {
         _channels.AddOrUpdate(
            channel.ChannelAnnouncement.ShortChannelId,
            channel, (id, gossipChannel) => channel);
         
         return Task.FromResult(channel);
      }

      public Task<GossipChannel?> GetGossipChannelAsync(ShortChannelId shortChannelId)
      {
         _channels.TryGetValue(shortChannelId, out GossipChannel? channel);
         
         return Task.FromResult(channel);
      }

      public Task RemoveGossipChannelsAsync(params ShortChannelId[] channelIds)
      {
         foreach (var shortChannelId in channelIds)
         {
            if (_channels.TryRemove(shortChannelId, out var channel))
               throw new InvalidOperationException();
         }
         return Task.CompletedTask;
      }

      public bool IsNodeInBlacklistedList(PublicKey nodeId) => _blacklistedNodeDictionary.Contains(nodeId);

      public Task AddNodeToBlacklistAsync(params PublicKey[] publicKeys)
      {
         foreach (var publicKey in publicKeys)
         {
            if (_blacklistedNodeDictionary.Contains(publicKey))
               continue;
            _blacklistedNodeDictionary.Add(publicKey);      
         }
         return Task.CompletedTask;
      }
   }
}