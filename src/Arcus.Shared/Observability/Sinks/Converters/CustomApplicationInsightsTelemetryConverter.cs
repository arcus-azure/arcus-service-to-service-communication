using System;
using System.Collections.Generic;
using Arcus.Observability.Telemetry.Core;
using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Converters;
using Arcus.Shared.Observability.Sinks.Configuration;
using GuardNet;
using Microsoft.ApplicationInsights.Channel;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.Sinks.ApplicationInsights.TelemetryConverters;
using CustomDependencyTelemetryConverter = Arcus.Shared.Observability.Sinks.Converters.Dependencies.CustomDependencyTelemetryConverter;
using EventTelemetryConverter = Serilog.Sinks.ApplicationInsights.Sinks.ApplicationInsights.TelemetryConverters.EventTelemetryConverter;
using HttpDependencyTelemetryConverter = Arcus.Shared.Observability.Sinks.Converters.Dependencies.HttpDependencyTelemetryConverter;
using SqlDependencyTelemetryConverter = Arcus.Shared.Observability.Sinks.Converters.Dependencies.SqlDependencyTelemetryConverter;
using TraceTelemetryConverter = Serilog.Sinks.ApplicationInsights.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter;

namespace Arcus.Shared.Observability.Sinks.Converters
{
    /// <summary>
    /// Represents a general conversion from Serilog <see cref="LogEvent"/> instances to Application Insights <see cref="ITelemetry"/> instances.
    /// </summary>
    public class CustomApplicationInsightsTelemetryConverter : TelemetryConverterBase
    {
        private readonly SuperCustomExceptionTelemetryConverter _exceptionTelemetryConverter;
        private readonly TraceTelemetryConverter _traceTelemetryConverter = new TraceTelemetryConverter();
        private readonly EventTelemetryConverter _eventTelemetryConverter = new EventTelemetryConverter();
        private readonly MetricTelemetryConverter _metricTelemetryConverter = new MetricTelemetryConverter();
        private readonly SuperCustomRequestTelemetryConverter _requestTelemetryConverter = new SuperCustomRequestTelemetryConverter();
        
        private readonly CustomDependencyTelemetryConverter _superCustomDependencyTelemetryConverter = 
            new CustomDependencyTelemetryConverter();

        private readonly HttpDependencyTelemetryConverter _httpDependencyTelemetryConverter =
            new HttpDependencyTelemetryConverter();

        private readonly SqlDependencyTelemetryConverter _sqlDependencyTelemetryConverter =
            new SqlDependencyTelemetryConverter();

        private CustomApplicationInsightsTelemetryConverter(ApplicationInsightsSinkOptions options)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of options to influence how to track to Application Insights");
            _exceptionTelemetryConverter = new SuperCustomExceptionTelemetryConverter(options.Exception);
        }
        
        /// <summary>
        ///     Convert the given <paramref name="logEvent"/> to a series of <see cref="ITelemetry"/> instances.
        /// </summary>
        /// <param name="logEvent">The event containing all relevant information for an <see cref="ITelemetry"/> instance.</param>
        /// <param name="formatProvider">The instance to control formatting.</param>
        public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider)
        {
            if (logEvent.Exception != null)
            {
                return _exceptionTelemetryConverter.Convert(logEvent, formatProvider);
            }

            if (logEvent.MessageTemplate.Text.StartsWith(MessagePrefixes.RequestViaHttp))
            {
                return _requestTelemetryConverter.Convert(logEvent, formatProvider);
            }

            if (logEvent.MessageTemplate.Text.StartsWith(MessagePrefixes.Dependency))
            {
                return _superCustomDependencyTelemetryConverter.Convert(logEvent, formatProvider);
            }

            if (logEvent.MessageTemplate.Text.StartsWith(MessagePrefixes.DependencyViaHttp))
            {
                return _httpDependencyTelemetryConverter.Convert(logEvent, formatProvider);
            }

            if (logEvent.MessageTemplate.Text.StartsWith(MessagePrefixes.DependencyViaSql))
            {
                return _sqlDependencyTelemetryConverter.Convert(logEvent, formatProvider);
            }

            if (logEvent.MessageTemplate.Text.StartsWith(MessagePrefixes.Event))
            {
                return _eventTelemetryConverter.Convert(logEvent, formatProvider);
            }

            if (logEvent.MessageTemplate.Text.StartsWith(MessagePrefixes.Metric))
            {
                return _metricTelemetryConverter.Convert(logEvent, formatProvider);
            }

            return _traceTelemetryConverter.Convert(logEvent, formatProvider);
        }

        /// <summary>
        ///     Creates an instance of the converter
        /// </summary>
        public static CustomApplicationInsightsTelemetryConverter Create()
        {
            return Create(new ApplicationInsightsSinkOptions());
        }

        /// <summary>
        /// Create an instance of the <see cref="CustomApplicationInsightsTelemetryConverter"/> class.
        /// </summary>
        /// <param name="options">The optional user-defined configuration options to influence the tracking behavior to Azure Application Insights.</param>
        public static CustomApplicationInsightsTelemetryConverter Create(ApplicationInsightsSinkOptions options)
        {
            return new CustomApplicationInsightsTelemetryConverter(options ?? new ApplicationInsightsSinkOptions());
        }
    }
}