using System;
using System.Buffers;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Types.Serialization
{
    public class NetworkMessageSerializer<TMessage> : INetworkMessageSerializer
        where TMessage : NetworkMessageBase, new()
    {
        private readonly IProtocolTypeSerializer<TMessage> _serializer;

        private TMessage _message;

        public NetworkMessageSerializer(IProtocolTypeSerializer<TMessage> serializer)
        {
            _serializer = serializer;
            _message = new TMessage();
        }

        public bool CanSerialize(string command)
        {
            return _message.Command == command;
        }

        public NetworkMessageBase Deserialize(ref SequenceReader<byte> reader)
        {
            return _serializer.Deserialize(ref reader,0);
        }

        public byte[] Serialize(NetworkMessageBase message)
        {
            if (message is not TMessage messageBase)
                throw new InvalidCastException();

            var buffer = new ArrayBufferWriter<byte>();

            _serializer.Serialize(messageBase, 0, buffer);

            return buffer.WrittenMemory.ToArray();
        }
    }
}