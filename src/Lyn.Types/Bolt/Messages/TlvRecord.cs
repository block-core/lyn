namespace Lyn.Types.Bolt.Messages
{
   public class TlvRecord
   {
      public ulong Type { get; set; }

      public ulong Size { get; set; }

      /// <summary>
      /// In cases where the tlv record is unknown
      /// this field can be used to hold the payload
      /// </summary>
      public byte[]? Payload { get; set; }
   }
}