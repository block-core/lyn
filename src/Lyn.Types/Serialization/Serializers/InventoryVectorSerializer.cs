using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
   public class InventoryVectorSerializer : IProtocolTypeSerializer<InventoryVector>
   {
      private readonly IProtocolTypeSerializer<UInt256> _uInt256Serializator;

      public InventoryVectorSerializer(IProtocolTypeSerializer<UInt256> uInt256Serializator)
      {
         _uInt256Serializator = uInt256Serializator;
      }

      public InventoryVector Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new InventoryVector
         {
            Type = reader.ReadUInt(),
            Hash = reader.ReadWithSerializer(protocolVersion, _uInt256Serializator),
         };
      }

      public int Serialize(InventoryVector typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         int size = 0;
         size += writer.WriteUInt(typeInstance.Type);
         size += writer.WriteWithSerializer(typeInstance.Hash, protocolVersion, _uInt256Serializator);

         return size;
      }
   }
}
