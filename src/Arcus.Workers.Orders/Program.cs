using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Security.Core;
using Arcus.Shared;
using Arcus.Shared.Messages;
using Arcus.Shared.Services;
using Arcus.Shared.Services.Interfaces;
using Arcus.Workers.Orders.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Extensions.Hosting;

namespace Arcus.Workers.Orders
{
    public class Program
    {
        private const string ApplicationInsightsConnectionStringKeyName = "APPLICATIONINSIGHTS_CONNECTION_STRING";
        private const string ComponentName = "Order Worker";

        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateBootstrapLogger();
            
            try
            {
                IHost host = CreateHostBuilder(args).Build();
                await ConfigureSerilogAsync(host);
                await host.RunAsync();
                
                return 0;
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
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
                       .UseSerilog(Log.Logger)
                       .ConfigureServices((hostContext, services) =>
                       {
                           services.AddAppName("Order Worker");
                           services.AddAssemblyAppVersion<Program>();

                           services.AddHttpClient("Bacon API");
                           services.AddScoped<IBaconService, BaconService>();

                           services.AddServiceBusQueueMessagePump("orders", secretProvider => secretProvider.GetRawSecretAsync("SERVICEBUS_CONNECTIONSTRING"))
                                   .WithServiceBusMessageHandler<EatBaconRequestMessageHandler, EatBaconRequestMessage>();
                       });
        }

        private static async Task ConfigureSerilogAsync(IHost host)
        {
            var secretProvider = host.Services.GetRequiredService<ISecretProvider>();
            string connectionString = await secretProvider.GetRawSecretAsync(ApplicationInsightsConnectionStringKeyName);

            var reloadLogger = (ReloadableLogger) Log.Logger;
            reloadLogger.Reload(config =>
            {
                config.MinimumLevel.Information()
                      .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                      .Enrich.FromLogContext()
                      .Enrich.WithVersion(host.Services)
                      .Enrich.WithComponentName(host.Services)
                      .WriteTo.Console();
                
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    config.WriteTo.AzureApplicationInsightsWithConnectionString(host.Services, connectionString);
                }
                
                return config;
            });
        }
    }
}
