using Arcus.Observability.Telemetry.Core;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Arcus.Shared.Observability.Sinks.Converters
{
    /// <summary>
    /// Represents a conversion from the Operation-related logging information to the Application Insights <see cref="OperationContext"/> instance.
    /// </summary>
    public class SuperCustomOperationContextConverter
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

            if (telemetryEntry.Properties.TryGetValue(ContextProperties.Correlation.OperationId, out string correlationId))
            {
                string operationId = correlationId;
                string parentOperationId = string.Empty;

                // TODO: Contribute Upstream : Provide capability to interpret and pass parent id
                // This is a hack as a workaround so I can pass this without changing how we log things
                if (correlationId.Contains("//"))
                {
                    operationId = correlationId.Split("//")[0];
                    parentOperationId = correlationId.Split("//")[1];
                }

                telemetryEntry.Context.Operation.Id = operationId;
                telemetryEntry.Context.Operation.ParentId = parentOperationId;
            }
        }
    }
}
