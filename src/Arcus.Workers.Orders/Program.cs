using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Shared;
using Arcus.Shared.Messages;
using Arcus.Workers.Orders.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Arcus.Workers.Orders
{
    public class Program
    {
        private const string ComponentName = "Order Worker";

        public static int Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();

            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureAppConfiguration(configuration =>
                       {
                           configuration.AddCommandLine(args);
                           configuration.AddEnvironmentVariables();
                       })
                       .ConfigureSecretStore((config, stores) =>
                       {
                           stores.AddEnvironmentVariables();
                       })
                       .UseSerilog(DefineLoggerConfiguration)
                       .ConfigureServices((hostContext, services) =>
                       {
                           services.AddBaconApiIntegration();

                           services.AddServiceBusMessageRouting(serviceProvider =>
                           {
                               return new CustomAzureServiceBusMessageRouter(serviceProvider,
                                   new AzureServiceBusMessageRouterOptions(),
                                   serviceProvider.GetRequiredService<ILogger<AzureServiceBusMessageRouter>>());
                           });

                           services.AddServiceBusQueueMessagePump("orders", secretProvider => secretProvider.GetRawSecretAsync("SERVICEBUS_CONNECTIONSTRING"))
                                   .WithServiceBusMessageHandler<EatBaconRequestMessageHandler, EatBaconRequestMessage>();
                       });
        }

        private static void DefineLoggerConfiguration(HostBuilderContext context, IServiceProvider services, LoggerConfiguration loggerConfiguration)
        {
            SerilogFactory.ConfigureSerilog(ComponentName, loggerConfiguration, context.Configuration, services, useHttpCorrelation: false);
        }
    }
}
