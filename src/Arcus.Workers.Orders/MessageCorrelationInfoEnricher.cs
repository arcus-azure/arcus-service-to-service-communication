using System;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Arcus.Observability.Telemetry.Serilog.Enrichers;
using GuardNet;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.Messaging.Abstractions.Telemetry
{
    /// <summary>
    /// Logger enrichment of the <see cref="MessageCorrelationInfo" /> model.
    /// </summary>
    public class CustomMessageCorrelationInfoEnricher : ILogEventEnricher
    {
        private readonly MessageCorrelationInfo _correlationInfo;
        private const string CycleId = "CycleId";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Arcus.Observability.Telemetry.Serilog.Enrichers.CorrelationInfoEnricher`1" /> class.
        /// </summary>
        /// <param name="correlationInfoAccessor">The accessor implementation for the custom <see cref="T:Arcus.Observability.Correlation.CorrelationInfo" /> model.</param>
        public CustomMessageCorrelationInfoEnricher (MessageCorrelationInfo correlationInfo)
        {
            _correlationInfo = correlationInfo;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            Console.WriteLine($"Correlation: {_correlationInfo.OperationId}, {_correlationInfo.TransactionId}, {_correlationInfo.OperationParentId}");
            EnrichLogPropertyIfPresent(logEvent, propertyFactory, ContextProperties.Correlation.OperationId, _correlationInfo.OperationId);
            EnrichLogPropertyIfPresent(logEvent, propertyFactory, ContextProperties.Correlation.TransactionId, _correlationInfo.TransactionId);
            EnrichLogPropertyIfPresent(logEvent, propertyFactory, ContextProperties.Correlation.OperationParentId, _correlationInfo.OperationParentId);
            EnrichLogPropertyIfPresent(logEvent, propertyFactory, CycleId, _correlationInfo.CycleId);
        }

        protected void EnrichLogPropertyIfPresent(
            LogEvent logEvent,
            ILogEventPropertyFactory propertyFactory,
            string propertyName,
            string propertyValue)
        {
            LogEventProperty property = propertyFactory.CreateProperty(propertyName, (object) propertyValue);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
