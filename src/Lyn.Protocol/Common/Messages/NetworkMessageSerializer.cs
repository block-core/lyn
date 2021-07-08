using System;
using System.Buffers;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt1.TlvStreams;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Common.Messages
{
    public class NetworkMessageSerializer<TMessage> : INetworkMessageSerializer
        where TMessage : MessagePayload, new()
    {
        private readonly IProtocolTypeSerializer<TMessage> _serializer;
        private readonly ITlvStreamSerializer _tlvStreamSerializer;

        private TMessage _message;

        public NetworkMessageSerializer(IProtocolTypeSerializer<TMessage> serializer, ITlvStreamSerializer tlvStreamSerializer)
        {
            _serializer = serializer;
            _tlvStreamSerializer = tlvStreamSerializer;
            _message = new TMessage();
        }

        public bool CanSerialize(MessageType type)
        {
            return _message.MessageType == type;
        }

        public BoltMessage Deserialize(ref SequenceReader<byte> reader)
        {
            var payload = _serializer.Deserialize(ref reader);

            var tlv = _tlvStreamSerializer.DeserializeTlvStream(ref reader);
            
            return new BoltMessage
            { 
                Payload = payload,
                Extension = tlv
            };
        }

        public byte[] Serialize(BoltMessage message)
        {
            if (message.Payload is not TMessage messageBase)
                throw new InvalidCastException();

            var buffer = new ArrayBufferWriter<byte>();

            _serializer.Serialize(messageBase, buffer);
            _tlvStreamSerializer.SerializeTlvStream(message.Extension, buffer);

            return buffer.WrittenMemory.ToArray();
        }
    }
}