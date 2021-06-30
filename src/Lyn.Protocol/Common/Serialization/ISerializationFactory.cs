using Lyn.Types.Serialization;

namespace Lyn.Protocol.Common.Serialization
{
    public interface ISerializationFactory
    {
        byte[] Serialize<TMessage>(TMessage message, ProtocolTypeSerializerOptions? options = null);

        TMessage Deserialize<TMessage>(byte[] bytes, ProtocolTypeSerializerOptions? options = null);
    }
}