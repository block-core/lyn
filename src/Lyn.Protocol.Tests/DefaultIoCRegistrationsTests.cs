using System;
using System.Linq;
using System.Reflection;
using Lyn.Protocol.Common;
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

            foreach (var type in Assembly.GetAssembly(typeof(DefaultIoCRegistrations)).DefinedTypes
                .Where(_ => _.IsInterface && !_.IsGenericType)
                .Select(_ => _.AsType()))
            {
                var service = provider.GetService(type);

                if (service is null)
                {
                    throw new ArgumentOutOfRangeException(type.FullName);
                }
                
                Assert.NotNull(service);
            }
        }
        
        // [Fact]
        // public void IoCRegistrationsWithNoErrorsForGenerics()
        // {
        //     _serviceCollection
        //         .AddLogging()
        //         .AddLynComponents();
        //
        //     var provider = _serviceCollection.BuildServiceProvider();
        //
        //     foreach (var type in Assembly.GetAssembly(typeof(DefaultIoCRegistrations))
        //         .GetTypes()
        //         .Where(_ => _.IsInterface)
        //         .Select(_ => _.GetType()))
        //     {
        //         var service = provider.GetService(type);
        //
        //         if (service is null)
        //         {
        //             throw new ArgumentOutOfRangeException(type.FullName);
        //         }
        //         
        //         Assert.NotNull(service);
        //     }
        // }
    }
}