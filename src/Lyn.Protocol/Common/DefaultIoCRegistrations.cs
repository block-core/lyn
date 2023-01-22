using System;
using System.Linq;
using System.Reflection;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt1.Messages.TlvRecords;
using Lyn.Protocol.Bolt1.TlvStreams;
using Lyn.Protocol.Bolt2.ChannelClose;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Bolt2.Wallet;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt7;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Bolt8;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Hashing;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Lyn.Protocol.Common
{
    public static class DefaultIoCRegistrations
    {
        public static IServiceCollection AddLynComponents(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddGossipComponents()
                .AddSerializationComponents()
                .AddNetworkMessageSerialization()
                .AddNoiseComponents()
                .AddDefaultComponents()
                .AddControlAndSetupMessageSupport();
            return serviceCollection;
        }

        public static IServiceCollection AddSerializationComponents(this IServiceCollection services)
        {
            ScanAssemblyAndRegisterTypeSingleton(services, typeof(IProtocolTypeSerializer<>));

            ScanAssemblyAndRegisterTypeSingleton(services, typeof(IProtocolTypeSerializer<>), typeof(InitMessageSerializer).Assembly);

            services.AddTransient<ITlvRecordSerializer, NetworksTlvSerializer>();
            services.AddTransient<ITlvRecordSerializer, UpfrontShutdownScriptTlvSerializer>();

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

            services.AddSingleton<IChainConfigProvider, ChainConfigProvider>(); ;
            services.AddSingleton<ISecretStore, SecretStore>();

            services.AddTransient(typeof(IBoltMessageSender<>), typeof(BoltMessageSender<>));

            services.AddSingleton<INodeSettings, NodeSettings>();
            
            return services;
        }

        private static IServiceCollection AddNoiseComponents(this IServiceCollection services)
        {
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

            // Bolt 3
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<OpenChannel>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<AcceptChannel>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<FundingCreated>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<FundingSigned>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<FundingLocked>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<Shutdown>>();
            services.AddSingleton<INetworkMessageSerializer, NetworkMessageSerializer<ClosingSigned>>();

            return services;
        }

        private static IServiceCollection AddControlAndSetupMessageSupport(this IServiceCollection services)
        {
            services.AddSingleton<IPeerRepository, InMemoryPeerRepository>();
            services.AddSingleton<IPingPongMessageRepository, InMemoryPingPongMessageRepository>();
            services.AddTransient<IPingMessageAction, PingMessageService>();
            services.AddTransient<IInitMessageAction, InitMessageService>();

            services.AddTransient<ITlvStreamSerializer, TlvStreamSerializer>();

            services.AddSingleton<IStartOpenChannelService, StartOpenChannelService>(); //TODO Dan this is not control and setup services
            services.AddSingleton<IChannelCandidateRepository, InMemoryChannelCandidateRepository>();
            services.AddSingleton<IWalletTransactions, WalletTransactions>();
            services.AddSingleton<IPaymentChannelRepository,InMemoryPaymentChannelRepository>();
            services.AddSingleton<IShutdownAction, ShutdownMessageService>();
            services.AddSingleton<ICloseSignedAction, CloseChannelMessageService>();

            return services;
        }

        private static void ScanAssemblyAndRegisterTypeSingleton(IServiceCollection services, Type protocolSerializerInterface)
        {
            ScanAssemblyAndRegisterTypeSingleton(services, protocolSerializerInterface,
                protocolSerializerInterface.Assembly);
        }

        private static void ScanAssemblyAndRegisterTypeSingleton(IServiceCollection services, Type protocolSerializerInterface, Assembly assembly)
        {
            // Discovers and registers all type implementation in this assembly.
            var implementations = from type in assembly.GetTypes()
                                  from typeInterface in type.GetInterfaces()
                                  where typeInterface.IsGenericType &&
                                        protocolSerializerInterface.IsAssignableFrom(typeInterface.GetGenericTypeDefinition())
                                  select new { Interface = typeInterface, ImplementationType = type };

            foreach (var implementation in implementations)
            {
                services.AddSingleton(implementation.Interface, implementation.ImplementationType);
            }
        }
    }
}