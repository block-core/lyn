namespace Lyn.Types.Bolt.Messages
{
    public sealed class InitMessage : BoltMessage
    {
        private const string COMMAND = "16";

        public override string Command => COMMAND;

        public byte[] GlobalFeatures { get; set; } = new byte[0];
        public byte[] Features { get; set; } = new byte[0];
    }
}