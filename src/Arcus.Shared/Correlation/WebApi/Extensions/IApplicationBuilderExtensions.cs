using Arcus.Shared.Correlation.WebApi.Middleware;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extra extensions on the <see cref="IApplicationBuilder"/> for logging.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds operation and transaction correlation to the application by using the <see cref="CorrelationWithUpstreamApiGatewayMiddleware"/> in the request pipeline.
        /// </summary>
        /// <param name="app">The builder to configure the application's request pipeline.</param>
        public static IApplicationBuilder UseHttpCorrelationFromPoc(this IApplicationBuilder app)
        {
            Guard.NotNull(app, nameof(app));

            return app.UseMiddleware<CorrelationWithUpstreamApiGatewayMiddleware>();
        }
    }
}
