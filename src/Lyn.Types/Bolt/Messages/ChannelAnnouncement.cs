using Lyn.Types.Fundamental;

namespace Lyn.Types.Bolt.Messages
{
   public class ChannelAnnouncement : GossipBaseMessage
   {
      private const string COMMAND = "256";

      public ChannelAnnouncement()
      {
         NodeSignature1 = new CompressedSignature();
         NodeSignature2 = new CompressedSignature();
         BitcoinSignature1 = new CompressedSignature();
         BitcoinSignature2 = new CompressedSignature();
         Features = new byte[0];
         ChainHash = new ChainHash(new byte[32]);
         ShortChannelId = new ShortChannelId(new byte[8]);
         NodeId1 = new PublicKey();
         NodeId2 = new PublicKey();
         BitcoinKey1 = new PublicKey();
         BitcoinKey2 = new PublicKey();
      }

      public override string Command => COMMAND;
      
      public CompressedSignature NodeSignature1 { get; set; }
      
      public CompressedSignature NodeSignature2 { get; set; }
      
      public CompressedSignature BitcoinSignature1 { get; set; }
      
      public CompressedSignature BitcoinSignature2 { get; set; }

      public ushort Len { get; set; }

      public byte[] Features { get; set; }

      public ChainHash ChainHash { get; set; }

      public ShortChannelId ShortChannelId { get; set; }

      public PublicKey NodeId1 { get; set; }
      
      public PublicKey NodeId2 { get; set; }
      
      public PublicKey BitcoinKey1 { get; set; }
      
      public PublicKey BitcoinKey2 { get; set; }
   }
}