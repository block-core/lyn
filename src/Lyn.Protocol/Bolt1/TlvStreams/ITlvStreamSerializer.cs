using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt1.TlvStreams
{
    public interface ITlvStreamSerializer<TMessage> where TMessage : MessagePayload
    {
        bool TryGetType(ulong recordType, [MaybeNullWhen(false)] out ITlvRecordSerializer<TMessage> tlvRecordSerializer);

        void SerializeTlvStream(TlVStream? message, IBufferWriter<byte> output);

        TlVStream? DeserializeTlvStream(ref SequenceReader<byte> reader);
    }
}