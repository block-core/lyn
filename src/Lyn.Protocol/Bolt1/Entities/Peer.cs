namespace Lyn.Protocol.Bolt1.Entities
{
    public class Peer
    {
        public string PeerId { get; set; }

        public byte[] Featurs { get; set; }

        public byte[] GlobalFeatures { get; set; }
    }
}