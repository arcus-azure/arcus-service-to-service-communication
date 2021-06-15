using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Serilog.Events;

namespace Arcus.Shared.Observability.Sinks.Converters.Dependencies
{
    /// <summary>
    /// Represents a conversion from a Serilog <see cref="LogEvent"/> to an Application Insights <see cref="DependencyTelemetry"/> instance.
    /// </summary>
    public class HttpDependencyTelemetryConverter : SuperCustomDependencyTelemetryConverter
    {
        /// <summary>
        ///     Gets the custom dependency type name from the given <paramref name="logEvent"/> to use in an <see cref="DependencyTelemetry"/> instance.
        /// </summary>
        /// <param name="logEvent">The logged event.</param>
        protected override string GetDependencyType(LogEvent logEvent)
        {
            return DependencyType.Http.ToString();
        }
    }
}
