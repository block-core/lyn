namespace Lyn.Types.Bolt.Messages
{
    public class PongMessage : BoltMessage
    {
        private const string COMMAND = "19";
        public override string Command => COMMAND;

      public ushort BytesLen { get; set; }

      public byte[]? Ignored { get; set; }

      public ushort Id => BytesLen;
   }
}