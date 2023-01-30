using System.Buffers;
using Lyn.Protocol.Bolt1.TlvStreams;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelClose.Messages.TlvRecords
{
    public class FeeRangeSerializer : ITlvRecordSerializer<ClosingSigned>
    {
        public ulong RecordTlvType => 1;
        public void Serialize(TlvRecord message, IBufferWriter<byte> output)
        {
            output.WriteULong(((FeeRange)message).MinFeeRange);
            output.WriteULong(((FeeRange)message).MaxFeeRange);
        }

        public TlvRecord Deserialize(ref SequenceReader<byte> reader)
        {
            var record = new FeeRange { Type = RecordTlvType, Size = (ulong)reader.Remaining };

            record.MinFeeRange = reader.ReadULong();
            record.MaxFeeRange = reader.ReadULong();

            return record;
        }
    }
}