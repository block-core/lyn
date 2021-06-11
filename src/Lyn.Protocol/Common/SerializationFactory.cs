using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Lyn.Protocol.Common
{
    internal class SerializationFactory : ISerializationFactory
    {
        private readonly List<INetworkMessageSerializer> _messageSerializers;

        public SerializationFactory(IServiceProvider serviceProvider, IEnumerable<INetworkMessageSerializer> serializers)
        {
            _messageSerializers = serviceProvider.GetServices<INetworkMessageSerializer>()
                .ToList();
        }

        public byte[] Serialize<TMessage>(TMessage message) where TMessage : NetworkMessageBase
        {
            // var buffer = new ArrayBufferWriter<byte>();
            //
            // if (_serviceProvider.GetService(typeof(IProtocolTypeSerializer<TMessage>)) 
            //     is not IProtocolTypeSerializer<TMessage> serializer)
            //     throw new ArgumentException(typeof(TMessage).FullName);
            //
            // serializer.Serialize(message, 0, buffer);
            //
            // return buffer.WrittenMemory.ToArray();

            return _messageSerializers.First(_ => _.CanSerialize(message.Command))
                .Serialize(message);
        }
        //
        // public TMessage Deserialize<TMessage>(ref SequenceReader<byte> reader) where TMessage : NetworkMessageBase
        // {
        //     if (_serviceProvider.GetService(typeof(IProtocolTypeSerializer<TMessage>)) 
        //         is not IProtocolTypeSerializer<TMessage> serializer)
        //         throw new ArgumentException(typeof(TMessage).FullName);
        //
        //     return serializer.Deserialize(ref reader, 0);
        // }

        public NetworkMessageBase Deserialize(SequenceReader<byte> reader, string command)
        {
            return _messageSerializers.First(_ => _.CanSerialize(command))
                .Deserialize(ref reader);
        }
    }
}