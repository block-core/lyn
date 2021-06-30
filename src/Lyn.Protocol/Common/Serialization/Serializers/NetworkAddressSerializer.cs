using System;
using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class NetworkAddressSerializer : IProtocolTypeSerializer<NetworkAddress>
    {
        public NetworkAddress Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new NetworkAddress
            {
                // https://bitcoin.org/en/developer-reference#version
                Time = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt()),

                Services = reader.ReadULong(),
                IP = reader.ReadBytes(16).ToArray(),
                Port = reader.ReadUShort()
            };
        }

        public int Serialize(NetworkAddress typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int size = 0;
            // https://bitcoin.org/en/developer-reference#version
            size += writer.WriteUInt((uint)typeInstance.Time.ToUnixTimeSeconds());

            size += writer.WriteULong(typeInstance.Services);
            size += writer.WriteBytes(typeInstance.IP!);
            size += writer.WriteUShort(typeInstance.Port);

            return size;
        }
    }
}