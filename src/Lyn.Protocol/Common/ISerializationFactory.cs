using System.Buffers;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Common
{
    public interface ISerializationFactory
    {
        byte[] Serialize<TMessage>(TMessage message, ProtocolTypeSerializerOptions? options = null);

        TMessage Deserialize<TMessage>(byte[] bytes, ProtocolTypeSerializerOptions? options = null);
    }
}