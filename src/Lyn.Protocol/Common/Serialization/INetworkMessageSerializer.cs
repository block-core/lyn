using System.Buffers;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Common.Serialization
{
    public interface INetworkMessageSerializer
    {
        bool CanSerialize(string command);

        BoltMessage Deserialize(ref SequenceReader<byte> reader);

        byte[] Serialize(BoltMessage message);
    }
}