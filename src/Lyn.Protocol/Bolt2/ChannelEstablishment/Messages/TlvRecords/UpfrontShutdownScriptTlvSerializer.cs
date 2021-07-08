using System;
using System.Buffers;
using Lyn.Protocol.Bolt1.Messages.TlvRecords;
using Lyn.Protocol.Bolt1.TlvStreams;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords
{
    public class UpfrontShutdownScriptTlvSerializer : ITlvRecordSerializer
    {
        public Type GetRecordType() => typeof(UpfrontShutdownScriptTlvRecord);

        public ulong RecordTlvType => 0;

        public void Serialize(TlvRecord message, IBufferWriter<byte> output)
        {
            // for now just fill the buffer
            output.Write(message.Payload.AsSpan());
        }

        public TlvRecord Deserialize(ref SequenceReader<byte> reader)
        {
            var result = new UpfrontShutdownScriptTlvRecord { Type = RecordTlvType, Size = (ulong)reader.Remaining };

            result.Payload = reader.ReadBytes((int)reader.Remaining).ToArray();

            return result;
        }
    }
}