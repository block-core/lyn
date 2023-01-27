using System.Buffers;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.MessageRetransmission.Messages
{
    public class ChannelReestablishSerializer : IProtocolTypeSerializer<ChannelReestablish>
    {
        public int Serialize(ChannelReestablish typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId);
            size += writer.WriteULong(typeInstance.NextCommitmentNumber, true);
            size += writer.WriteULong(typeInstance.NextRevocationNumber, true);
            size += writer.WriteBytes(typeInstance.MyCurrentPerCommitmentPoint);
            size += writer.WriteBytes(typeInstance.YourLastPerCommitmentSecret);

            return size;
        }

        public ChannelReestablish Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new ChannelReestablish(reader.ReadUint256(true),
                reader.ReadULong(true),
                reader.ReadULong(true),
                reader.ReadBytes(PublicKey.LENGTH),
                new Secret(reader.ReadBytes(PrivateKey.LENGTH).ToArray()));
        }
    }
}