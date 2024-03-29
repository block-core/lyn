﻿using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class TransactionOutputSerializer : IProtocolTypeSerializer<TransactionOutput>
    {
        public TransactionOutput Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new TransactionOutput
            {
                Value = reader.ReadLong(),
                PublicKeyScript = reader.ReadByteArray()
            };
        }

        public int Serialize(TransactionOutput typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int size = writer.WriteLong(typeInstance.Value);
            size += writer.WriteByteArray(typeInstance.PublicKeyScript);

            return size;
        }
    }
}