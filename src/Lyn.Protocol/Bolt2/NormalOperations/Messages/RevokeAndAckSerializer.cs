using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class RevokeAndAckSerializer : IProtocolTypeSerializer<RevokeAndAck>
    {
        public int Serialize(RevokeAndAck typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = 0;

            bytesWritten += writer.WriteUint256(typeInstance.ChannelId!, false);
            bytesWritten += writer.WriteBytes(typeInstance.PerCommitmentSecret!.GetBytes().ToArray());
            bytesWritten += writer.WriteBytes(typeInstance.NextPerCommitmentPoint!.GetSpan().ToArray());

            return bytesWritten;
        }

        public RevokeAndAck Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            return new RevokeAndAck
            {
                ChannelId = reader.ReadUint256(),
                PerCommitmentSecret = new Secret(reader.ReadBytes(32).ToArray()),
                NextPerCommitmentPoint = new PublicKey(reader.ReadBytes(33).ToArray())
            };
        }
    }
}

