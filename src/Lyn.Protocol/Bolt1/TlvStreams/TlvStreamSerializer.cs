using System.Buffers;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Lyn.Types.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt1.TlvStreams
{
    public class TlvStreamSerializer : ITlvStreamSerializer
    {
        private const int MAX_RECORD_SIZE = 65535; // 65KB
        private readonly Dictionary<ulong, ITlvRecordSerializer> _tlvRecordTypeMappings;

        public TlvStreamSerializer(IEnumerable<ITlvRecordSerializer> recordSerializers)
        {
            _tlvRecordTypeMappings = recordSerializers.ToDictionary(serializer => serializer.RecordTlvType);
        }

        public bool TryGetType(ulong recordType, [MaybeNullWhen(false)] out ITlvRecordSerializer tlvRecordSerializer)
        {
            return _tlvRecordTypeMappings.TryGetValue(recordType, out tlvRecordSerializer);
        }

        public void SerializeTlvStream(TlVStream? message, IBufferWriter<byte> output)
        {
            if (message == null) return;

            TlvRecord? lastRecord = null;

            foreach (TlvRecord record in message.Records)
            {
                if (TryGetType(record.Type, out ITlvRecordSerializer? recordSerializer))
                {
                    output.WriteBigSize(record.Type);

                    if (lastRecord == null)
                    {
                        // the first record
                        lastRecord = record;
                    }
                    else
                    {
                        if (record.Type < lastRecord.Type)
                        {
                            // check records are in ascending order
                            throw new SerializationException("Tlv records not canonical");
                        }
                    }

                    if (record.Size > MAX_RECORD_SIZE)
                        throw new SerializationException("Record is too large");

                    output.WriteBigSize(record.Size);

                    recordSerializer.Serialize(record, output);
                }
                else
                {
                    // unknown type
                    throw new SerializationException("Unknown Tlv records type");
                }
            }
        }

        public TlVStream? DeserializeTlvStream(ref SequenceReader<byte> reader)
        {
            if (reader.Remaining <= 0) return null;

            var message = new TlVStream();

            while (reader.Remaining > 0)
            {
                ulong recordType = reader.ReadBigSize();
                ulong recordLength = reader.ReadBigSize();

                if (recordLength > MAX_RECORD_SIZE)
                {
                    // check the max size
                    throw new SerializationException("Record is too large");
                }

                if ((long)recordLength > reader.Remaining)
                {
                    // check the max size
                    throw new SerializationException("Record length exceeds the remaining message");
                }

                // check if known type
                if (TryGetType(recordType, out ITlvRecordSerializer? recordSerializer))
                {
                    // type known

                    ReadOnlySequence<byte> sequence = reader.Sequence.Slice(reader.Position, (int)recordLength);
                    var innerReader = new SequenceReader<byte>(sequence);

                    TlvRecord record = recordSerializer.Deserialize(ref innerReader);
                    message.Records.Add(record);

                    if (innerReader.Consumed != (long)recordLength)
                    {
                        throw new SerializationException("Record length inconsistent to tlv length");
                    }

                    reader.Advance((long)recordLength);
                }
                else
                {
                    // type unknown

                    if (recordType % 2 == 0)
                    {
                        //if even, throw
                        throw new MessageSerializationException("TlvSerialization error, sequence error");
                    }
                    else
                    {
                        message.Records.Add(new TlvRecord() { Type = recordType, Size = recordLength });

                        // read record value (we aren't interested in these bytes so we just advance)
                        reader.Advance((long)recordLength);
                    }
                }
            }

            return message;
        }
    }
}