using System;
using Arcus.Observability.Correlation;
using Arcus.POC.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds operation and transaction correlation to the application.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds operation and transaction correlation to the application.
        /// </summary>
        /// <param name="services">The services collection containing the dependency injection services.</param>
        /// <param name="configureOptions">The function to configure additional options how the correlation works.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddCustomHttpCorrelation(
            this IServiceCollection services,
            Action<CustomHttpCorrelationInfoOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the HTTP correlation services");

            services.AddHttpContextAccessor();
            services.AddCorrelation<CustomHttpCorrelationInfoAccessor, CorrelationInfo, CustomHttpCorrelationInfoOptions>(
                serviceProvider =>
                {
                    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                    return new CustomHttpCorrelationInfoAccessor(httpContextAccessor);
                },
                configureOptions);
            services.AddScoped(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<CustomHttpCorrelationInfoOptions>>();
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                var correlationInfoAccessor = serviceProvider.GetRequiredService<ICorrelationInfoAccessor>();
                var logger = serviceProvider.GetService<ILogger<CustomHttpCorrelation>>();
                
                return new CustomHttpCorrelation(options, httpContextAccessor, correlationInfoAccessor, logger);
            });

            return services;
        }
    }
}