namespace Lyn.Types.Bolt.Messages
{
   public class ErrorMessage : NetworkMessageBase
   {
      private const string COMMAND = "17";

      public ErrorMessage() => ChannelId = new byte[0];

      public override string Command => COMMAND;
         
      public byte[] ChannelId { get; set; }

      public ushort Len { get; set; }
      
      public byte[]? Data { get; set; }
   }
}