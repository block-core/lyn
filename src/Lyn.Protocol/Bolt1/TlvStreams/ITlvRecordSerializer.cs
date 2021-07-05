using System;
using System.Buffers;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt1.TlvStreams
{
    public interface ITlvRecordSerializer
    {
        Type GetRecordType();

        ulong RecordTlvType { get; }

        void Serialize(TlvRecord message, IBufferWriter<byte> output);

        TlvRecord Deserialize(ref SequenceReader<byte> reader);
    }
}