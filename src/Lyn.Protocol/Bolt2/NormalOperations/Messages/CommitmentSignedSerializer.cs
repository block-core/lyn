using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class CommitmentSignedSerializer : IProtocolTypeSerializer<CommitmentSigned>
    {
        public CommitmentSigned Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var channelId = reader.ReadUint256(false);
            ushort numHtlcs = reader.ReadUShort(true);

            var commitmentSigned = new CommitmentSigned
            {
                ChannelId = channelId,
                NumHtlcs = numHtlcs,
                Signature = new CompressedSignature(reader.ReadBytes(CompressedSignature.LENGTH).ToArray())
            };

            if (numHtlcs > 0)
            {
                int htlcSignatureLength = numHtlcs * CompressedSignature.LENGTH;
                commitmentSigned.HtlcSignature = reader.ReadBytes(htlcSignatureLength).ToArray();
            }

            return commitmentSigned;
        }

        public int Serialize(CommitmentSigned typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId ?? new UInt256(new byte[32]), false);
            size += writer.WriteUShort((ushort)(typeInstance.HtlcSignature?.Length / CompressedSignature.LENGTH ?? 0), true);
            if (typeInstance.Signature != null)
                size += writer.WriteBytes(typeInstance.Signature);
            if (typeInstance.HtlcSignature?.Length > 0)
                size += writer.WriteBytes(typeInstance.HtlcSignature);

            return size;
        }
    }
}
