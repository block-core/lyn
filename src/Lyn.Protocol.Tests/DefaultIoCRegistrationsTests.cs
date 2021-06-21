using System;
using System.Linq;
using System.Reflection;
using Lyn.Protocol.Common;
using Lyn.Protocol.Connection;
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
        public void IoCRegistrationsWithNoErrors()
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
    }
}