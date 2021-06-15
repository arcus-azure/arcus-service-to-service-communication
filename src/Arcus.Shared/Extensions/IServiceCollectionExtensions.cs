using Arcus.Shared.ApplicationInsights;
using Arcus.Shared.Services;
using Arcus.Shared.Services.Interfaces;
using Microsoft.ApplicationInsights.Extensibility;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static void AddApplicationInsights(this IServiceCollection services, string componentName)
        {
            services.AddApplicationInsightsTelemetry();
            services.AddSingleton<ITelemetryInitializer, CloudRoleNameTelemetryInitializer>(serviceProvider => CloudRoleNameTelemetryInitializer.CreateForComponent(componentName));
        }

        public static void AddBaconApiIntegration(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddScoped<IBaconService, BaconService>();
        }
    }
}
