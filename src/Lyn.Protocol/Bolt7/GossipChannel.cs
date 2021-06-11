using System;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7
{
   public class GossipChannel
   {
      private readonly LocalNode? _localNode;

      public GossipChannel(ChannelAnnouncement channelAnnouncement, LocalNode? localNode)
      {
         ChannelAnnouncement = channelAnnouncement;
         _localNode = localNode;
      }

      public ChannelAnnouncement ChannelAnnouncement { get; set; }

      public bool IsChannelWithLocalNode() => _localNode != null;

      public PublicKey GetRemoteNodeId() => _localNode == null
         ? throw new InvalidOperationException() //TODO David check if better to return nullable
         : _localNode == LocalNode.Node1
            ? ChannelAnnouncement.NodeId2 : ChannelAnnouncement.NodeId1;

      public PublicKey GetRemoteBitcoinAddress() => _localNode == null
         ? throw new InvalidOperationException()
         : _localNode == LocalNode.Node1
            ? ChannelAnnouncement.BitcoinKey2 : ChannelAnnouncement.BitcoinKey1;
      
      public enum LocalNode
      {
         Node1,
         Node2
      }
   }
}