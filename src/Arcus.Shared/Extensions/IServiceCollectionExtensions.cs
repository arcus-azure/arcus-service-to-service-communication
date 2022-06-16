using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Arcus.Shared.Services;
using Arcus.Shared.Services.Interfaces;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static void AddBaconApiIntegration(this IServiceCollection services)
        {
            services.AddHttpClient();

            // TODO: Contribute Upstream - HTTP service-to-service automagical tracking - Option #1 - Use HTTP pipeline, if we can fix the DI scoping for all scenarios
            // Remove name requirement as this is an extension workaround
            // https://thomaslevesque.com/2016/12/08/fun-with-the-httpclient-pipeline/
            // services.AddTransient<DependencyTrackingHttpHandler>();
            //services.AddHttpClient("SomeName")
                      //.AddHttpMessageHandler(serviceProvider => serviceProvider.GetRequiredService<DependencyTrackingHttpHandler>());

            services.AddScoped<IBaconService, BaconService>();
        }
    }
    
    // This helps us do automagic tracking of HTTP dependencies
    public class DependencyTrackingHttpHandler : DelegatingHandler
    {
        private readonly ICorrelationInfoAccessor _correlationInfoAccessor;
        private readonly ILogger<DependencyTrackingHttpHandler> _logger;

        public DependencyTrackingHttpHandler(ICorrelationInfoAccessor correlationInfoAccessor, ILogger<DependencyTrackingHttpHandler> logger)
        {
            _correlationInfoAccessor = correlationInfoAccessor;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (var httpDependencyMeasurement = DurationMeasurement.Start())
            {
                var newDependencyId = Guid.NewGuid().ToString();
                var correlationInfo = _correlationInfoAccessor.GetCorrelationInfo();
                var upstreamOperationParentId = $"|{correlationInfo?.OperationId}.{newDependencyId}";

                request.Headers.Add("Request-Id", upstreamOperationParentId);
                request.Headers.Add("X-Transaction-ID", correlationInfo?.TransactionId);

                var response = await base.SendAsync(request, cancellationToken);

                _logger.LogHttpDependency(request, response.StatusCode, httpDependencyMeasurement, dependencyId: upstreamOperationParentId);

                return response;
            }
        }
    }
}
