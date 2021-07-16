using System.Buffers;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Tests
{
    public static class SerializerHelperExtentions
    {
        public static byte[] SerializeHelper<TProtocolType>(this IProtocolTypeSerializer<TProtocolType> ser, TProtocolType typeInstance, ProtocolTypeSerializerOptions? options = null)
        {
            var buffer = new ArrayBufferWriter<byte>();
            ser.Serialize(typeInstance, buffer, options);
            return buffer.WrittenSpan.ToArray();
        }

        public static TProtocolType DeserializeHelper<TProtocolType>(this IProtocolTypeSerializer<TProtocolType> ser, byte[] rawBytes, ProtocolTypeSerializerOptions? options = null)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(rawBytes));
            var res = ser.Deserialize(ref reader, options);
            return res;
        }
    }
}