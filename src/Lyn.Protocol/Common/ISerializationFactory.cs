using System.Buffers;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Common
{
    public interface ISerializationFactory
    {
        byte[] Serialize<TMessage>(TMessage message) where TMessage : NetworkMessageBase;
        
        //TMessage Deserialize<TMessage>(ref SequenceReader<byte> reader) where TMessage : NetworkMessageBase;

        NetworkMessageBase Deserialize(SequenceReader<byte> reader, string command);
    }
}