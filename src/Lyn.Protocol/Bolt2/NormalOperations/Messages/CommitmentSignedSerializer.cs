using System;
using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.NormalOperations.Messages
{
    public class CommitmentSignedSerializer : IProtocolTypeSerializer<CommitmentSigned>
    {
        public int Serialize(CommitmentSigned typeInstance, IBufferWriter<byte> writer,
            ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = 0;

            bytesWritten += writer.WriteUint256(typeInstance.ChannelId!, false);
            bytesWritten += writer.WriteBytes(typeInstance.Signature!.GetBytes());
            bytesWritten += writer.WriteUShort(typeInstance.NumHtlcs ?? 0, true);
            
            // Write HTLC signatures if present
            if (typeInstance.NumHtlcs > 0 && typeInstance.HtlcSignatures != null)
            {
                foreach (var sig in typeInstance.HtlcSignatures)
                {
                    bytesWritten += writer.WriteBytes(sig.GetBytes());
                }
            }

            return bytesWritten;
        }

        public CommitmentSigned Deserialize(ref SequenceReader<byte> reader,
            ProtocolTypeSerializerOptions? options = null)
        {
            var commitmentSigned = new CommitmentSigned
            {
                ChannelId = reader.ReadUint256(),
                Signature = new CompressedSignature(reader.ReadBytes(64).ToArray()),
                NumHtlcs = reader.ReadUShort(true)
            };

            if (commitmentSigned.NumHtlcs > 0)
            {
                commitmentSigned.HtlcSignatures = new CompressedSignature[commitmentSigned.NumHtlcs.Value];
                for (int i = 0; i < commitmentSigned.NumHtlcs; i++)
                {
                    commitmentSigned.HtlcSignatures[i] = new CompressedSignature(reader.ReadBytes(64).ToArray());
                }
            }

            return commitmentSigned;
        }
    }
}

