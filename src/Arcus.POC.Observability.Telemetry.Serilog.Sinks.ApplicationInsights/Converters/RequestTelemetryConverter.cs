using System;
using System.Collections.Generic;
using Arcus.Observability.Telemetry.Core;
using Arcus.POC.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Extensions;
using GuardNet;
using Microsoft.ApplicationInsights.DataContracts;
using Serilog.Events;

namespace Arcus.POC.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Converters
{
    /// <summary>
    /// Represents a conversion from a Serilog <see cref="LogEvent"/> to an Application Insights <see cref="RequestTelemetry"/> instance.
    /// </summary>
    public class RequestTelemetryConverter : CustomTelemetryConverter<RequestTelemetry>
    {
        /// <summary>
        ///     Creates a telemetry entry for a given log event
        /// </summary>
        /// <param name="logEvent">Event that was logged and written to this sink</param>
        /// <param name="formatProvider">Provider to format event</param>
        /// <returns>Telemetry entry to emit to Azure Application Insights</returns>
        protected override RequestTelemetry CreateTelemetryEntry(LogEvent logEvent, IFormatProvider formatProvider)
        {
            Guard.NotNull(logEvent, nameof(logEvent), "Requires a Serilog log event to create an Azure Application Insights Request telemetry instance");
            Guard.NotNull(logEvent.Properties, nameof(logEvent), "Requires a Serilog event with a set of properties to create an Azure Application Insights Request telemetry instance");
            
            StructureValue logEntry = logEvent.Properties.GetAsStructureValue(ContextProperties.RequestTracking.RequestLogEntry);
            string requestHost = logEntry.Properties.GetAsRawString(nameof(ExtendedRequestLogEntry.RequestHost));
            string requestUri = logEntry.Properties.GetAsRawString(nameof(ExtendedRequestLogEntry.RequestUri));
            string responseStatusCode = logEntry.Properties.GetAsRawString(nameof(ExtendedRequestLogEntry.ResponseStatusCode));
            TimeSpan requestDuration = logEntry.Properties.GetAsTimeSpan(nameof(ExtendedRequestLogEntry.RequestDuration));
            DateTimeOffset requestTime = logEntry.Properties.GetAsDateTimeOffset(nameof(ExtendedRequestLogEntry.RequestTime));
            IDictionary<string, string> context = logEntry.Properties.GetAsDictionary(nameof(ExtendedRequestLogEntry.Context));
            string sourceSystem = logEntry.Properties.GetAsRawString(nameof(ExtendedRequestLogEntry.SourceSystem));
            string sourceName = logEntry.Properties.GetAsRawString(nameof(ExtendedRequestLogEntry.SourceName));

            // TODO: Generate this
            string requestTelemetryId = Guid.NewGuid().ToString();
            
            bool isSuccessfulRequest = DetermineRequestOutcome(responseStatusCode);
            
            var url = DetermineUrl(sourceSystem, requestHost, requestUri);
            var source = DetermineRequestSource(sourceSystem, sourceName, context);

            var requestTelemetry = new RequestTelemetry(sourceName, requestTime, requestDuration, responseStatusCode, isSuccessfulRequest)
            {
                Id = requestTelemetryId,
                Url = url,
                Source = source
            };

            requestTelemetry.Properties.AddRange(context);
            return requestTelemetry;
        }

        private string DetermineRequestSource(string sourceSystem, string sourceName, IDictionary<string, string> context)
        {
            if (sourceSystem != "Azure Service Bus")
            {
                return null;
            }

            var entityName = context["ServiceBus-Entity"];
            var namespaceEndpoint = context["ServiceBus-Endpoint"];
            return $"type:Azure Service Bus | name:{entityName} | endpoint:sb://{namespaceEndpoint}.servicebus.windows.net/";
        }

        private static Uri DetermineUrl(string sourceSystem, string requestHost, string requestUri)
        {
            if (sourceSystem != "HTTP")
            {
                return null;
            }

            return new Uri($"{requestHost}{requestUri}");
        }

        private static bool DetermineRequestOutcome(string rawResponseStatusCode)
        {
            var statusCode = int.Parse(rawResponseStatusCode);

            return statusCode >= 200 && statusCode < 300;
        }
    }
}
