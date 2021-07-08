using System.Buffers;
using Lyn.Protocol.Bolt1.Messages;

namespace Lyn.Protocol.Common.Messages
{
    public interface INetworkMessageSerializer
    {
        bool CanSerialize(MessageType type);

        BoltMessage Deserialize(ref SequenceReader<byte> reader);

        byte[] Serialize(BoltMessage message);
    }
}