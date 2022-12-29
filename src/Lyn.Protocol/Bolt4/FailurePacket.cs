using System.Buffers;
using System.Linq;
using Lyn.Protocol.Bolt4.Messages;
using Lyn.Protocol.Common.Crypto;

namespace Lyn.Protocol.Bolt4
{

    // todo: this class doesn't feel very C#-y.  It's a mix of static and instance methods, 
    // and it's not clear what the purpose of the instance methods are.
    public class FailurePacket
    {
        public const int MaxPayloadLength = 256;
        public const int PacketLength = 32 + MaxPayloadLength + 2 + 2;

        private readonly ISphinx sphinx;

        // todo: Do not love this Sphinx layering, need to resolve
        public FailurePacket(ISphinx sphinx)
        {
            this.sphinx = sphinx;
        }

        public byte[] Create(byte[] sharedSecret, FailureMessage failureMessage)
        {
            var um = sphinx.GenerateSphinxKey("um", sharedSecret);
            var messageSerializer = new FailureMessageSerializer();

            // create a bufferwriter to write the failure message to
            var bufferWriter = new ArrayBufferWriter<byte>();
            int bytesWritten = messageSerializer.Serialize(failureMessage, bufferWriter);
            
            // var failureOnion = new FailureOnion();

            return null;
        }

        public byte[] Wrap(byte[] packet, byte[] sharedSecret)
        {
            if (packet.Length != PacketLength)
            {
                // this is a warn in eclair
                // throw new ArgumentException($"Invalid error packet length {packet.Length}, must be {PacketLength} (malicious or buggy downstream node)");
            }

            var key = sphinx.GenerateSphinxKey("ammag", sharedSecret);
            var stream = sphinx.GenerateStream(key, packet.Length);

            var paddedPacket = new byte[packet.Length].Concat(packet).ToArray();
            var result = sphinx.ExclusiveOR(paddedPacket, stream).ToArray();
            return result;
        }

    }

}