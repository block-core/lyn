using Lyn.Types.Serialization;
using System;
using System.Buffers;

namespace Lyn.Protocol.Bolt4.Messages
{

    /**
    * An onion-encrypted failure from an intermediate node:
    * {{{
    * +----------------+----------------------------------+-----------------+----------------------+-----+
    * | HMAC(32 bytes) | failure message length (2 bytes) | failure message | pad length (2 bytes) | pad |
    * +----------------+----------------------------------+-----------------+----------------------+-----+
    * }}}
    * with failure message length + pad length = 256
    */
    public record FailureOnion(byte[] Hmac, ushort FailureMessageLength, byte[] FailureMessage, ushort PadLength, byte[] Pad);

    public class FailureOnionSerializer : IProtocolTypeSerializer<FailureOnion>
    {

        public FailureOnionSerializer()
        {
        }

        public int Serialize(FailureOnion typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int bytesWritten = 0;
            bytesWritten += writer.WriteBytes(typeInstance.Hmac);
            bytesWritten += writer.WriteUShort(typeInstance.FailureMessageLength);
            bytesWritten += writer.WriteBytes(typeInstance.FailureMessage);
            bytesWritten += writer.WriteUShort(typeInstance.PadLength);
            bytesWritten += writer.WriteBytes(typeInstance.Pad);
            return bytesWritten;
        }

        public FailureOnion Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            // readi in the hmac first
            var hmac = reader.ReadBytes(32).ToArray();

            // next read the failure message length
            var failureMessageLength = reader.ReadUShort();

            // next read the failure message
            var failureMessageBytes = reader.ReadBytes(failureMessageLength).ToArray();

            // next read the pad length
            var padLength = reader.ReadUShort();

            // next read the pad
            var pad = reader.ReadBytes(padLength).ToArray();

            return new FailureOnion(hmac, failureMessageLength, failureMessageBytes, padLength, pad);
        }
    }
}