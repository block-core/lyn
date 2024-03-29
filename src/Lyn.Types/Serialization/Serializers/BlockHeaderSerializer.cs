﻿using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class BlockHeaderSerializer : IProtocolTypeSerializer<BlockHeader>
    {
        private readonly IProtocolTypeSerializer<UInt256> _uInt256Serializator;

        public BlockHeaderSerializer(IProtocolTypeSerializer<UInt256> uInt256Serializator)
        {
            _uInt256Serializator = uInt256Serializator;
        }

        public BlockHeader Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            bool headerInBlock = options?.HasOption(SerializerOptions.HEADER_IN_BLOCK) ?? false;

            var header = new BlockHeader
            {
                Version = reader.ReadInt(),
                PreviousBlockHash = reader.ReadWithSerializer(_uInt256Serializator),
                MerkleRoot = reader.ReadWithSerializer(_uInt256Serializator),
                TimeStamp = reader.ReadUInt(),
                Bits = reader.ReadUInt(),
                Nonce = reader.ReadUInt(),
            };

            if (!headerInBlock)
            {
                // when we are deserializing an header (from Headers message) we need to consume the VarInt anyway to let the sequence advance.
                _ = reader.ReadVarInt();
            }

            return header;
        }

        public int Serialize(BlockHeader typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            bool headerInBlock = options?.HasOption(SerializerOptions.HEADER_IN_BLOCK) ?? false;

            int size = 0;
            size += writer.WriteInt(typeInstance.Version);
            size += writer.WriteWithSerializer(typeInstance.PreviousBlockHash!, _uInt256Serializator);
            size += writer.WriteWithSerializer(typeInstance.MerkleRoot!, _uInt256Serializator);
            size += writer.WriteUInt(typeInstance.TimeStamp);
            size += writer.WriteUInt(typeInstance.Bits);
            size += writer.WriteUInt(typeInstance.Nonce);

            // serialized headers contains this byte, while header serialized as block doesn't because it's serialized as part of the transactions array
            if (!headerInBlock)
            {
                // protocol still expect this value to be set for Headers payload
                size += writer.WriteVarInt(0);
            }

            return size;
        }
    }
}