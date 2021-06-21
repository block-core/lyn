namespace Lyn.Types.Bolt.Messages
{
    public class ErrorMessage : BoltMessage
    {
        private const string COMMAND = "17";

      public override string Command => COMMAND;

      public ChannelId ChannelId { get; set; } = new(new byte[0]);

      public ushort Len { get; set; }
      
      public byte[]? Data { get; set; }
   }
}