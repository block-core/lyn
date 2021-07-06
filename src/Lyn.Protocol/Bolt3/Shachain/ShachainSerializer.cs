using System.Buffers;
using Lyn.Types.Bitcoin;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt3.Shachain
{
    public class ShachainSerializer : IProtocolTypeSerializer<ShachainItems>
    {
        public int Serialize(ShachainItems typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            throw new System.NotImplementedException();
        }

        public ShachainItems Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            throw new System.NotImplementedException();
        }
    }
}