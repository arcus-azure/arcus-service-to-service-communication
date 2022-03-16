using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using System;
using Arcus.Custom;

namespace Arcus.Shared
{
    public class SerilogFactory
    {
        private const string ApplicationInsightsInstrumentationKeyName = "APPINSIGHTS_INSTRUMENTATIONKEY";

        public static void ConfigureSerilog(string componentName, LoggerConfiguration loggerConfiguration, IConfiguration configuration, IServiceProvider serviceProvider, bool useHttpCorrelation = true)
        {
            var instrumentationKey = configuration.GetValue<string>(ApplicationInsightsInstrumentationKeyName);

            loggerConfiguration = loggerConfiguration.MinimumLevel.Verbose()
                                                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                                                    .Enrich.FromLogContext()
                                                    .Enrich.WithVersion()
                                                    .Enrich.WithComponentName(componentName);

            if (useHttpCorrelation)
            {
                loggerConfiguration = loggerConfiguration.Enrich.WithCustomHttpCorrelationInfo(serviceProvider);
            }
            else
            {
                loggerConfiguration = loggerConfiguration.Enrich.WithCorrelationInfo(serviceProvider);
            }

            loggerConfiguration = loggerConfiguration.WriteTo.Console()
                .WriteTo.CustomAzureApplicationInsights(instrumentationKey);
        }
    }
}
