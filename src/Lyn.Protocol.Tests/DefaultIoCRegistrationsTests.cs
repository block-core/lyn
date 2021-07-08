using System.Linq;
using System.Reflection;
using FluentAssertions;
using Lyn.Protocol.Common;
using Lyn.Protocol.Connection;
using Lyn.Types.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyn.Protocol.Tests
{
    public class DefaultIoCRegistrationsTests
    {
        private IServiceCollection _serviceCollection;

        public DefaultIoCRegistrationsTests()
        {
            _serviceCollection = new ServiceCollection();
        }

        [Fact]
        public void IoCRegistrationsGetsAllMessageServicesWithNoErrors()
        {
            _serviceCollection
                .AddLogging()
                .AddLynComponents();

            var provider = _serviceCollection.BuildServiceProvider();

            var boltMessageType = typeof(IBoltMessageService<>);
            
            foreach (var type in Assembly.GetAssembly(typeof(DefaultIoCRegistrations)).DefinedTypes
                .Where(_ => _.IsClass && 
                                _.ImplementedInterfaces.Any(interfaceMapping => 
                            interfaceMapping.IsGenericType &&
                            interfaceMapping.GetGenericTypeDefinition() == boltMessageType))
                .Select(_ => _.GetInterface(boltMessageType.Name)))
            {
                var service = provider.GetService(type);
                
                Assert.NotNull(service);
            }
        }
        
        [Fact]
        public void IoCRegistrationsGetsAllSerializersWithNoErrors()
        {
            _serviceCollection
                .AddLogging()
                .AddLynComponents();

            var provider = _serviceCollection.BuildServiceProvider();

            var boltMessageType = typeof(IProtocolTypeSerializer<>);
            
            foreach (var type in Assembly.GetAssembly(typeof(DefaultIoCRegistrations)).DefinedTypes
                .Where(_ => _.IsClass && 
                            _.ImplementedInterfaces.Any(interfaceMapping => 
                                interfaceMapping.IsGenericType &&
                                interfaceMapping.GetGenericTypeDefinition() == boltMessageType))
                .Select(_ => _.GetInterface(boltMessageType.Name)))
            {
                var service = provider.GetService(type);

                service.Should()
                    .NotBeNull("{0}", type.FullName);
            }
        }
    }
}