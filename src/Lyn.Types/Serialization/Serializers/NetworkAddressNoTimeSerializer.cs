using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class NetworkAddressNoTimeSerializer : IProtocolTypeSerializer<NetworkAddressNoTime>
    {
        public NetworkAddressNoTime Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
        {
            return new NetworkAddressNoTime { Services = reader.ReadULong(), IP = reader.ReadBytes(16).ToArray(), Port = reader.ReadUShort() };
        }

        public int Serialize(NetworkAddressNoTime typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int size = 0;
            size += writer.WriteULong(typeInstance.Services);
            size += writer.WriteBytes(typeInstance.IP!);
            size += writer.WriteUShort(typeInstance.Port);

            return size;
        }
    }
}