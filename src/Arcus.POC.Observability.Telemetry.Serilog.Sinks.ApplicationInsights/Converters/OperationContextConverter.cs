using Arcus.Observability.Telemetry.Core;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Arcus.POC.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Converters
{
    /// <summary>
    /// Represents a conversion from the Operation-related logging information to the Application Insights <see cref="OperationContext"/> instance.
    /// </summary>
    public class OperationContextConverter
    {
        /// <summary>
        /// Enrich the given <paramref name="telemetryEntry"/> with the Operation-related information.
        /// </summary>
        /// <param name="telemetryEntry">The telemetry instance to enrich.</param>
        public void EnrichWithCorrelationInfo<TEntry>(TEntry telemetryEntry) where TEntry : ITelemetry, ISupportProperties
        {
            if (telemetryEntry?.Context?.Operation == null)
            {
                return;
            }

            if (telemetryEntry.Properties.TryGetValue(ContextProperties.Correlation.OperationId, out string operationId))
            {
                telemetryEntry.Context.Operation.Id = operationId;
            }

            if (telemetryEntry.Properties.TryGetValue(ContextProperties.Correlation.OperationParentId, out string operationParentId))
            {
                telemetryEntry.Context.Operation.ParentId = operationParentId;
            }

            // TODO: Contribute Upstream
            // This gives the operation a nice name to provide structure in the performance overview
            if (telemetryEntry is RequestTelemetry f)
            {
                f.Context.Operation.Name = f.Name;
            }

            if (telemetryEntry is DependencyTelemetry d)
            {
                d.Context.Operation.Name = d.Name;
            }

            if (telemetryEntry is EventTelemetry e)
            {
                e.Context.Operation.Name = e.Name;
            }

            if (telemetryEntry is AvailabilityTelemetry a)
            {
                a.Context.Operation.Name = a.Name;
            }

            if (telemetryEntry is MetricTelemetry m)
            {
                m.Context.Operation.Name = m.Name;
            }
        }
    }
}
