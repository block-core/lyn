using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class ReplyChannelRangeSerializer : IProtocolTypeSerializer<ReplyChannelRange>
    {
        public ReplyChannelRange Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var chainHash = reader.ReadUint256(false);
            uint firstBlocknum = reader.ReadUInt(true);
            uint numberOfBlocks = reader.ReadUInt(true);
            bool complete = reader.ReadBool();
            ushort len = reader.ReadUShort(true);

            return new ReplyChannelRange(
                chainHash, firstBlocknum, numberOfBlocks, complete,
                len > 0 ? reader.ReadBytes(len).ToArray() : new byte[0])
            {
                Len = len
            };
        }

        public int Serialize(ReplyChannelRange typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChainHash, false);
            size += writer.WriteUInt(typeInstance.FirstBlocknum, true);
            size += writer.WriteUInt(typeInstance.NumberOfBlocks, true);
            size += writer.WriteBool(typeInstance.Complete);
            size += writer.WriteUShort((ushort)(typeInstance.EncodedShortIds?.Length ?? 0), true);
            if (typeInstance.EncodedShortIds?.Length > 0)
                size += writer.WriteBytes(typeInstance.EncodedShortIds);

            return size;
        }
    }
}
