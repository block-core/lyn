using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt1.Messages
{
    public class ErrorMessageSerializer : IProtocolTypeSerializer<ErrorMessage>
    {
        public int Serialize(ErrorMessage typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteUint256(typeInstance.ChannelId);
            size += writer.WriteUShort(typeInstance.Len, true);
            if (typeInstance.Data != null)
            {
                size += writer.WriteBytes(typeInstance.Data);    
            }
            
            return size;
        }

        public ErrorMessage Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var channelId = reader.ReadBytes(32).ToArray();
            ushort len = reader.ReadUShort(true);

            return new ErrorMessage
            {
                ChannelId = new UInt256(channelId), 
                Len = len, 
                Data = reader.ReadBytes(len).ToArray()
            };
        }
    }
}