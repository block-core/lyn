using System.Buffers;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Types.Serialization
{
    public interface INetworkMessageSerializer
    {
        bool CanSerialize(string command);

        BoltMessage Deserialize(ref SequenceReader<byte> reader);

        byte[] Serialize(BoltMessage message);
    }
}