using System.Buffers;

namespace Lyn.Types.Serialization
{
    public interface IProtocolTypeSerializer<TProtocolType>
    {
        /// <summary>
        /// Serializes the specified protocol data type writing it into <paramref name="writer"/>.
        /// </summary>
        /// <param name="typeInstance">The type to serialize.</param>
        /// <param name="writer"></param>
        /// <param name="options"></param>
        /// <param name="output">The output buffer used to store data into.</param>
        /// <returns>number of written bytes</returns>
        int Serialize(TProtocolType typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null);

        /// <summary>
        /// Deserializes the specified message reading it from the <paramref name="reader" />.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="options"></param>
        /// <returns>number of read bytes</returns>
        TProtocolType Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null);
    }
}