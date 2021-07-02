namespace Lyn.Types.Bolt.Messages
{
    public class PingMessage : BoltMessage
    {
        public const ushort MAX_BYTES_LEN = 4096;// 65531; TODO David check bigger messages

        public PingMessage()
        { }

        public PingMessage(byte[] ignored)
        {
            BytesLen = (ushort)ignored.Length;
            Ignored = ignored;
        }

        public PingMessage(ushort bytesLen)
        {
            Ignored = new byte[bytesLen];
            BytesLen = bytesLen;
            NumPongBytes = (ushort)(MAX_BYTES_LEN - bytesLen);
        }

      private const string COMMAND = "18";
      
      public override string Command => COMMAND;

      public ushort NumPongBytes { get; set; }

      public ushort BytesLen { get; set; }

      public byte[] Ignored { get; set; }

      public ushort PongId => NumPongBytes;
    }
}