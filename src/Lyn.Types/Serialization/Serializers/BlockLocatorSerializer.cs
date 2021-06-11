using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
   public class BlockLocatorSerializer : IProtocolTypeSerializer<BlockLocator>
   {
      private readonly IProtocolTypeSerializer<UInt256> _uInt256Serializator;

      public BlockLocatorSerializer(IProtocolTypeSerializer<UInt256> uInt256Serializator)
      {
         _uInt256Serializator = uInt256Serializator;
      }

      public BlockLocator Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new BlockLocator { BlockLocatorHashes = reader.ReadArray(protocolVersion, _uInt256Serializator) };
      }

      public int Serialize(BlockLocator typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         return writer.WriteArray(typeInstance.BlockLocatorHashes, protocolVersion, _uInt256Serializator);
      }
   }
}
