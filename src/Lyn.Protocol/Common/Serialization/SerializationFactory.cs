using System;
using System.Buffers;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Common.Serialization
{
    public class SerializationFactory : ISerializationFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SerializationFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public byte[] Serialize<TMessage>(TMessage message, ProtocolTypeSerializerOptions? options = null)
        {
            if (_serviceProvider.GetService(typeof(IProtocolTypeSerializer<TMessage>))
                is not IProtocolTypeSerializer<TMessage> serializer)
                throw new ArgumentException(typeof(TMessage).FullName);

            var buffer = new ArrayBufferWriter<byte>();

            serializer.Serialize(message, buffer, options);

            return buffer.WrittenMemory.ToArray();
        }

        public TMessage Deserialize<TMessage>(byte[] bytes, ProtocolTypeSerializerOptions? options = null)
        {
            if (_serviceProvider.GetService(typeof(IProtocolTypeSerializer<TMessage>))
                is not IProtocolTypeSerializer<TMessage> serializer)
                throw new ArgumentException(typeof(TMessage).FullName);

            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));

            return serializer.Deserialize(ref reader, options);
        }
    }
}