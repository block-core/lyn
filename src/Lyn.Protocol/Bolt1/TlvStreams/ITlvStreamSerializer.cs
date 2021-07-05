using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Lyn.Protocol.Bolt1.Messages;

namespace Lyn.Protocol.Bolt1.TlvStreams
{
    public interface ITlvStreamSerializer
    {
        bool TryGetType(ulong recordType, [MaybeNullWhen(false)] out ITlvRecordSerializer tlvRecordSerializer);

        void SerializeTlvStream(TlVStream? message, IBufferWriter<byte> output);

        TlVStream? DeserializeTlvStream(ref SequenceReader<byte> reader);
    }
}