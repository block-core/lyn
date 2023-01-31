using System.Buffers;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class FundingLockedSerializer : IProtocolTypeSerializer<FundingLocked>
    {
        public int Serialize(FundingLocked typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;

            size += writer.WriteUint256(typeInstance.ChannelId); //TODO find what the bug in uint256 is
            size += writer.WriteBytes(typeInstance.NextPerCommitmentPoint);
            
            return size;
        }

        public FundingLocked Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            return new FundingLocked
            {
                ChannelId = reader.ReadUint256(),
                NextPerCommitmentPoint = reader.ReadBytes(PublicKey.LENGTH)
            };
        }
    }
}