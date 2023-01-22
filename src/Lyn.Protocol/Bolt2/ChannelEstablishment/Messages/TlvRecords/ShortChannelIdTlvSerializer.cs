using System;
using System.Buffers;
using Lyn.Protocol.Bolt1.TlvStreams;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords
{
    public class ShortChannelIdTlvSerializer : ITlvRecordSerializer
    {
        public Type GetRecordType() => typeof(ShortChannelIdTlvRecord);

        public ulong RecordTlvType => 1; //TODO need to fix the logic to get the record to have same type on different messages
        public void Serialize(TlvRecord message, IBufferWriter<byte> output)
        {
            output.Write(message.Payload.AsSpan());
        }

        public TlvRecord Deserialize(ref SequenceReader<byte> reader)
        {
            var result = new ShortChannelIdTlvRecord { Type = RecordTlvType, Size = (ulong)reader.Remaining };

            result.Alias = reader.ReadBytes((int)reader.Remaining).ToArray();

            return result;
        }
    }
}