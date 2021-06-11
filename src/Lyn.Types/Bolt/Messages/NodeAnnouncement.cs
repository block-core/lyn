using Lyn.Types.Fundamental;

namespace Lyn.Types.Bolt.Messages
{
   public class NodeAnnouncement : GossipBaseMessage
   {
      private const string COMMAND = "257";

      public NodeAnnouncement()
      {
         Signature = new CompressedSignature();
         Len = 0;
         Features = new byte[0];
         Timestamp = 0;
         NodeId = new PublicKey();
         RgbColor = new byte[0];
         Alias = new byte[0];
         Addrlen = 0;
         Addresses = new byte[0];
      }

      public override string Command => COMMAND;

      public CompressedSignature Signature { get; set; }

      public ushort Len { get; set; }

      public byte[] Features { get; set; }

      public uint Timestamp { get; set; }

      public PublicKey NodeId { get; set; }

      public byte[] RgbColor { get; set; }

      public byte[] Alias { get; set; }

      public ushort Addrlen { get; set; }

      public byte[] Addresses { get; set; }
   }
}