using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;
using Arcus.Shared.Logging.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Configuration;

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
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

                loggerConfiguration = loggerConfiguration.Enrich.WithCorrelationInfo(new CustomHttpCorrelationInfoAccessor(httpContextAccessor));
            }

            loggerConfiguration = loggerConfiguration.WriteTo.Console()
                                                     .WriteTo.AzureApplicationInsightsWithInstrumentationKey("973f6a63-486e-4c73-9c45-1b920bdd6107");                                                  
        }
    }
}
