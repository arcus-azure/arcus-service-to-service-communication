using Arcus.Shared.Services;
using Arcus.Shared.Services.Interfaces;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static void AddBaconApiIntegration(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddScoped<IBaconService, BaconService>();
        }
    }
}
