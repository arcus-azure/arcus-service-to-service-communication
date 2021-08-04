using System;
using Arcus.POC.Messaging.Abstractions;
using Arcus.Shared;
using Arcus.Shared.Messages;
using Arcus.Workers.Orders.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                           // TODO: Fix the correlation retrieval in Bacon service
                           services.AddBaconApiIntegration();

                           // TODO: Import Arcus Messaging to add request tracking
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
