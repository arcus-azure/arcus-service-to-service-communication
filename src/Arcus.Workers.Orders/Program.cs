using Arcus.Shared.ApplicationInsights;
using Arcus.Shared.Messages;
using Arcus.Workers.Orders.MessageHandlers;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                       .ConfigureServices((hostContext, services) =>
                       {
                           services.AddBaconApiIntegration();
                           services.AddApplicationInsightsTelemetryWorkerService();
                           services.AddSingleton<ITelemetryInitializer, CloudRoleNameTelemetryInitializer>(serviceProvider => CloudRoleNameTelemetryInitializer.CreateForComponent(ComponentName));

                           services.AddServiceBusQueueMessagePump("orders", secretProvider => secretProvider.GetRawSecretAsync("SERVICEBUS_CONNECTIONSTRING"))
                                   .WithServiceBusMessageHandler<EatBaconRequestMessageHandler, EatBaconRequestMessage>();
                       });
        }
    }
}
