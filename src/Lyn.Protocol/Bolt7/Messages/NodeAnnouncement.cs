using Lyn.Protocol.Common.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class NodeAnnouncement : GossipMessage
    {
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

        public override MessageType MessageType => MessageType.NodeAnnouncement;

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