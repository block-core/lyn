using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class RevokeAndAckSerializer : IProtocolTypeSerializer<RevokeAndAck>
    {
        public RevokeAndAck Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new RevokeAndAck
            {
                ChannelId = reader.ReadUint256(false),
                PerCommitmentSecret = new Secret(reader.ReadBytes(32).ToArray()),
                NextPerCommitmentPoint = reader.ReadBytes(33).ToArray()
            };
        }

        public int Serialize(RevokeAndAck typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId ?? new UInt256(new byte[32]), false);
            size += writer.WriteBytes(typeInstance.PerCommitmentSecret ?? new Secret(new byte[32]));
            size += writer.WriteBytes(typeInstance.NextPerCommitmentPoint ?? new PublicKey());

            return size;
        }
    }
}
