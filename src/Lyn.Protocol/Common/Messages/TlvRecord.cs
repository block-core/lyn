namespace Lyn.Protocol.Common.Messages
{
   public class TlvRecord
   {
      public virtual ulong Type { get; set; }

      public virtual ulong Size { get; set; }

      /// <summary>
      /// In cases where the tlv record is unknown
      /// this field can be used to hold the payload
      /// </summary>
      public byte[]? Payload { get; set; }
   }
}