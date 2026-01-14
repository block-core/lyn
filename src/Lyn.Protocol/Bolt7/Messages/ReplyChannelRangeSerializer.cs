using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt7.Messages
{
    public class ReplyChannelRangeSerializer : IProtocolTypeSerializer<ReplyChannelRange>
    {
        public int Serialize(ReplyChannelRange typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = 0;

            bytesWritten += writer.WriteUint256(typeInstance.ChainHash!, false);
            bytesWritten += writer.WriteUInt(typeInstance.FirstBlocknum, true);
            bytesWritten += writer.WriteUInt(typeInstance.NumberOfBlocks, true);
            bytesWritten += writer.WriteByte(typeInstance.Complete);
            bytesWritten += writer.WriteUShort(typeInstance.Len, true);
            
            if (typeInstance.EncodedShortIds != null && typeInstance.EncodedShortIds.Length > 0)
            {
                bytesWritten += writer.WriteBytes(typeInstance.EncodedShortIds);
            }

            return bytesWritten;
        }

        public ReplyChannelRange Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            var chainHash = reader.ReadUint256();
            var firstBlocknum = reader.ReadUInt(true);
            var numberOfBlocks = reader.ReadUInt(true);
            var complete = reader.ReadByte();
            var len = reader.ReadUShort(true);
            var encodedShortIds = len > 0 ? reader.ReadBytes(len).ToArray() : null;

            return new ReplyChannelRange
            {
                ChainHash = chainHash,
                FirstBlocknum = firstBlocknum,
                NumberOfBlocks = numberOfBlocks,
                Complete = complete,
                Len = len,
                EncodedShortIds = encodedShortIds
            };
        }
    }
}

