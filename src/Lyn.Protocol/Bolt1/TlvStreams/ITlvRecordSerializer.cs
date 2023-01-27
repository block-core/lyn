using System;
using System.Buffers;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt1.TlvStreams
{
    public interface ITlvRecordSerializer<TMessageContext> where TMessageContext : MessagePayload
    {
        ulong RecordTlvType { get; }

        void Serialize(TlvRecord message, IBufferWriter<byte> output);

        TlvRecord Deserialize(ref SequenceReader<byte> reader);
    }
}