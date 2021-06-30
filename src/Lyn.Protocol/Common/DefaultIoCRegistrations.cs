using System;
using System.Linq;
using System.Reflection;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt2;
using Lyn.Protocol.Bolt2.ChannelEstablishment;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt7;
using Lyn.Protocol.Bolt8;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Lyn.Protocol.Common
{
    public static class DefaultIoCRegistrations
    {
        public static void AddLynComponents(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddGossipComponents()
                .AddSerializationComponents()
                .AddNetworkMessageSerialization()
                .AddNoiseComponents()
                .AddDefaultComponents()
                .AddControlAndSetupMessageSupport();
        }

        public static IServiceCollection AddSerializationComponents(this IServiceCollection services)
        {
            ScanAssemblyAndRegisterTypeSingleton(services, typeof(IProtocolTypeSerializer<>));

            return services;
        }

        private static IServiceCollection AddGossipComponents(this IServiceCollection services)
        {
            ScanAssemblyAndRegisterTypeSingleton(services, typeof(IBoltMessageService<>));
            ScanAssemblyAndRegisterTypeSingleton(services, typeof(IMessageValidator<>));

            return services;
        }

        private static IServiceCollection AddDefaultComponents(this IServiceCollection services)
        {
            services.AddSingleton<IGossipRepository, InMemoryGossipRepository>();
            services.AddTransient<IValidationHelper, ValidationHelper>();
            services.AddTransient<INoiseMessageTransformer, NoiseMessageTransformer>();
            services.AddSingleton<IRandomNumberGenerator, DefaultRandomNumberGenerator>();
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddSingleton<ISerializationFactory, SerializationFactory>();
            services.AddSingleton<ITransactionHashCalculator, TransactionHashCalculator>();

            services.AddSingleton<ILightningTransactions, LightningTransactions>();
            services.AddSingleton<ILightningKeyDerivation, LightningKeyDerivation>();
            services.AddSingleton<ILightningScripts, LightningScripts>();
            services.AddSingleton<IBoltFeatures, LynImplementedBoltFeatures>();
            services.AddSingleton<IParseFeatureFlags, ParseFeatureFlags>();

            services.AddSingleton<IChannelConfigProvider, ChannelConfigProvider>();
            services.AddSingleton<IChainConfigProvider, ChainConfigProvider>(); ;
            services.AddSingleton<ISecretStore, SecretStore>();

            return services;
        }

        private static IServiceCollection AddNoiseComponents(this IServiceCollection services)
        {
            //services.AddSingleton<IHandshakeStateFactory, HandshakeStateFactory>();
            services.AddSingleton<IEllipticCurveActions, EllipticCurveActions>();
            services.AddTransient<IHashWithState, HashWithState>();
            services.AddSingleton<IHkdf, Hkdf>();
            services.AddTransient<ICipherFunction, ChaCha20Poly1305CipherFunction>();
            services.AddSingleton<INoiseHashFunction, Sha256>();
            services.AddTransient<INoiseMessageTransformer, NoiseMessageTransformer>();
            services.AddSingleton<IKeyGenerator, KeyGenerator>();
            services.AddSingleton<IHandshakeService, HandshakeService>();
            return services;
        }

        private static IServiceCollection AddNetworkMessageSerialization(this IServiceCollection services)
        {
            //TODO David check if it should be singleton

            //Bolt 7
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<AnnouncementSignatures>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<ChannelAnnouncement>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<GossipTimestampFilter>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<NodeAnnouncement>>();

            // Bolt 1
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<InitMessage>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<ErrorMessage>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<PingMessage>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<PongMessage>>();

            return services;
        }

        private static IServiceCollection AddControlAndSetupMessageSupport(this IServiceCollection services)
        {
            services.AddSingleton<IPeerRepository, InMemoryPeerRepository>();
            services.AddSingleton<IPingPongMessageRepository, InMemoryPingPongMessageRepository>();
            services.AddTransient(typeof(IBoltMessageSender<>), typeof(BoltMessageSender<>));

            services.AddSingleton<IStartOpenChannelService, StartOpenChannelService>(); //TODO Dan this is not control and setup services
            services.AddSingleton<IChannelStateRepository, InMemoryChannelStateRepository>();
 
            return services;
        }
        
        
        private static void ScanAssemblyAndRegisterTypeSingleton(IServiceCollection services, Type protocolSerializerInterface)
        {
            // Discovers and registers all type implementation in this assembly.
            var implementations = from type in protocolSerializerInterface.Assembly.GetTypes()
                from typeInterface in type.GetInterfaces()
                where typeInterface.IsGenericType &&
                      protocolSerializerInterface.IsAssignableFrom(typeInterface.GetGenericTypeDefinition())
                select new {Interface = typeInterface, ImplementationType = type};

            foreach (var implementation in implementations)
            {
                services.AddSingleton(implementation.Interface, implementation.ImplementationType);
            }
        }
    }
}