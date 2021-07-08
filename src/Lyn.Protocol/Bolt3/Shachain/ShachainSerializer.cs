using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt3.Shachain
{
    public class ShachainSerializer : IProtocolTypeSerializer<ShachainItems>
    {
        public int Serialize(ShachainItems typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var size = 0;
            size += writer.WriteULong(typeInstance.Index);
            size += writer.WriteByte((byte) typeInstance.Secrets.Count);
            foreach (var (key, value) in typeInstance.Secrets)
            {
                size += writer.WriteInt(key);
                size += writer.WriteUint256(value.Secret);
                size += writer.WriteULong(value.Index);
            }

            return size;
        }

        public ShachainItems Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            var chainItems = new ShachainItems
            {
                Index = reader.ReadULong() 
            };
            
            var numberOfItems = reader.ReadByte();

            for (var i = 0; i < numberOfItems; i++)
            {
                chainItems.Secrets.Add(reader.ReadInt(), // secrets instantiated already with max size for the protocol 
                    new ShachainItem(reader.ReadUint256(), reader.ReadULong()));
            }

            return chainItems;
        }
    }
}