using Arcus.Observability.Telemetry.Core;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using System;

namespace Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Converters
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

            // TODO: differentiate between request and other types of telemetry:
            // 1. request telemetry should use operation ID as telemetry ID and
            // 2. other types should use operation ID as parent ID.
            if (telemetryEntry is RequestTelemetry requestTelemetry)
            {
                if (telemetryEntry.Properties.TryGetValue(ContextProperties.Correlation.OperationId, out string operationId))
                {
                    requestTelemetry.Id = operationId ?? $"generated-{Guid.NewGuid()}";
                }

                if (telemetryEntry.Properties.TryGetValue(ContextProperties.Correlation.TransactionId, out string transactionId))
                {
                    telemetryEntry.Context.Operation.Id = transactionId;
                }

                if (telemetryEntry.Properties.TryGetValue(ContextProperties.Correlation.OperationParentId, out string operationParentId))
                {
                    telemetryEntry.Context.Operation.ParentId = operationParentId;
                }
            }
            else
            {
                if (telemetryEntry.Properties.TryGetValue(ContextProperties.Correlation.TransactionId, out string transactionId))
                {
                    telemetryEntry.Context.Operation.Id = transactionId;
                }

                if (telemetryEntry.Properties.TryGetValue(ContextProperties.Correlation.OperationId, out string operationId))
                {
                    telemetryEntry.Context.Operation.ParentId = operationId;
                }
            }
        }

        /// <summary>
        /// Enrich the given <paramref name="telemetryEntry"/> with the operation name.
        /// </summary>
        /// <param name="telemetryEntry">The telemetry instance to enrich.</param>
        public void EnrichWithOperationName<TEntry>(TEntry telemetryEntry) where TEntry : ITelemetry, ISupportProperties
        {
            if (telemetryEntry is RequestTelemetry requestTelemetry)
            {
                // Check if operation has already been set with a custom value in the RequestTelemetryConverter, if not use the request name
                if (String.IsNullOrEmpty(requestTelemetry.Context.Operation.Name))
                {
                    requestTelemetry.Context.Operation.Name = requestTelemetry.Name;
                }
            }

            if (telemetryEntry is DependencyTelemetry dependencyTelemetry)
            {
                dependencyTelemetry.Context.Operation.Name = dependencyTelemetry.Name;
            }

            if (telemetryEntry is EventTelemetry eventTelemetry)
            {
                eventTelemetry.Context.Operation.Name = eventTelemetry.Name;
            }

            if (telemetryEntry is AvailabilityTelemetry availabilityTelemetry)
            {
                availabilityTelemetry.Context.Operation.Name = availabilityTelemetry.Name;
            }

            if (telemetryEntry is MetricTelemetry metricTelemetry)
            {
                metricTelemetry.Context.Operation.Name = metricTelemetry.Name;
            }
        }
    }
}
