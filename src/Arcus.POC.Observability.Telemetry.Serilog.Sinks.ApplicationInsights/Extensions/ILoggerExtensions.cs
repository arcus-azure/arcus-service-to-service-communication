using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Arcus.Observability.Telemetry.Core;
using Arcus.Observability.Telemetry.Core.Logging;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.POC.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Extensions
{
    // TODO: Cleanup or contribute upstream
    public static class ILoggerExtensions
    {
        /// <summary>
        ///     Logs an HTTP dependency
        /// </summary>
        /// <param name="logger">The logger to track the telemetry.</param>
        /// <param name="request">Request that started the HTTP communication</param>
        /// <param name="statusCode">Status code that was returned by the service for this HTTP communication</param>
        /// <param name="measurement">Measuring the latency of the HTTP dependency</param>
        /// <param name="context">Context that provides more insights on the dependency that was measured</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/>, <paramref name="request"/>, or <paramref name="measurement"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="request"/> doesn't have a request URI or HTTP method, the <paramref name="statusCode"/> is outside the bounds of the enumeration.
        /// </exception>
        public static void LogExtendedHttpDependency(
            this ILogger logger,
            HttpRequestMessage request,
            HttpStatusCode statusCode,
            DependencyMeasurement measurement,
            Dictionary<string, object> context = null,
            string dependencyId = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request message to track a HTTP dependency");
            Guard.NotNull(measurement, nameof(measurement), "Requires a dependency measurement instance to track the latency of the HTTP communication when tracking a HTTP dependency");
            Guard.For(() => !Enum.IsDefined(typeof(HttpStatusCode), statusCode),
                new ArgumentException("Requires a response HTTP status code that's within the bound of the enumeration to track a HTTP dependency"));

            LogExtendedHttpDependency(logger, request, statusCode, measurement.StartTime, measurement.Elapsed, context, dependencyId);
        }

        /// <summary>
        /// Gets the message format to log HTTP dependencies; compatible with Application Insights 'Dependencies'.
        /// </summary>
        public const string HttpDependencyFormat = "{@" + ContextProperties.DependencyTracking.DependencyLogEntry + "}";

        /// <summary>
        ///     Logs an HTTP dependency
        /// </summary>
        /// <param name="logger">The logger to track the telemetry.</param>
        /// <param name="request">Request that started the HTTP communication</param>
        /// <param name="statusCode">Status code that was returned by the service for this HTTP communication</param>
        /// <param name="startTime">Point in time when the interaction with the HTTP dependency was started</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="context">Context that provides more insights on the dependency that was measured</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> or <paramref name="request"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="duration"/> is a negative time range.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="request"/> doesn't have a request URI or HTTP method, the <paramref name="statusCode"/> is outside the bounds of the enumeration.
        /// </exception>
        public static void LogExtendedHttpDependency(
            this ILogger logger,
            HttpRequestMessage request,
            HttpStatusCode statusCode,
            DateTimeOffset startTime,
            TimeSpan duration,
            Dictionary<string, object> context = null,
            string dependencyId = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request message to track a HTTP dependency");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the HTTP dependency operation");
            Guard.For(() => request.RequestUri is null, new ArgumentException("Requires a HTTP request URI to track a HTTP dependency", nameof(request)));
            Guard.For(() => request.Method is null, new ArgumentException("Requires a HTTP request method to track a HTTP dependency", nameof(request)));
            Guard.For(() => !Enum.IsDefined(typeof(HttpStatusCode), statusCode),
                new ArgumentException("Requires a response HTTP status code that's within the bound of the enumeration to track a HTTP dependency"));

            context = context ?? new Dictionary<string, object>();

            Uri requestUri = request.RequestUri;
            string targetName = requestUri.Host;
            HttpMethod requestMethod = request.Method;
            dependencyId = string.IsNullOrWhiteSpace(dependencyId) ? Guid.NewGuid().ToString() : dependencyId;
            string dependencyName = $"{requestMethod} {requestUri.AbsolutePath}";
            bool isSuccessful = (int)statusCode >= 200 && (int)statusCode < 300;

            logger.LogWarning(HttpDependencyFormat, new ExtendedDependencyLogEntry(
                dependencyType: "Http",
                dependencyId: dependencyId,
                dependencyName: dependencyName,
                dependencyData: null,
                targetName: targetName,
                duration: duration,
                startTime: startTime,
                resultCode: (int)statusCode,
                isSuccessful: isSuccessful,
                context: context));
        }
    }
    /// <summary>
    /// Represents a custom dependency as a logging entry.
    /// </summary>
    public class ExtendedDependencyLogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyLogEntry"/> class.
        /// </summary>
        /// <param name="dependencyType">The custom type of dependency.</param>
        /// <param name="dependencyName">The name of the dependency.</param>
        /// <param name="dependencyData">The custom data of dependency.</param>
        /// <param name="targetName">The name of dependency target.</param>
        /// <param name="resultCode">The code of the result of the interaction with the dependency.</param>
        /// <param name="isSuccessful">The indication whether or not the operation was successful.</param>
        /// <param name="startTime">The point in time when the interaction with the HTTP dependency was started.</param>
        /// <param name="duration">The duration of the operation.</param>
        /// <param name="context">The context that provides more insights on the dependency that was measured.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="dependencyData"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="dependencyData"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="duration"/> is a negative time range.</exception>
        public ExtendedDependencyLogEntry(
            string dependencyType,
            string dependencyId,
            string dependencyName,
            object dependencyData,
            string targetName,
            TimeSpan duration,
            DateTimeOffset startTime,
            int? resultCode,
            bool isSuccessful,
            IDictionary<string, object> context)
        {
            Guard.NotNullOrWhitespace(dependencyId, nameof(dependencyId), "Requires a non-blank custom dependency type when tracking the custom dependency");
            Guard.NotNullOrWhitespace(dependencyType, nameof(dependencyType), "Requires a non-blank custom dependency type when tracking the custom dependency");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the dependency operation");

            DependencyType = dependencyType;
            DependencyId = dependencyId;
            DependencyName = dependencyName;
            DependencyData = dependencyData;
            TargetName = targetName;
            ResultCode = resultCode;
            IsSuccessful = isSuccessful;

            StartTime = startTime.ToString(FormatSpecifiers.InvariantTimestampFormat);
            Duration = duration;
            Context = context;
            Context[ContextProperties.General.TelemetryType] = TelemetryType.Dependency;
        }

        /// <summary>
        /// Gets the custom type of the dependency.
        /// </summary>
        public string DependencyType { get; }

        /// <summary>
        /// Gets the id of the dependency interaction.
        /// </summary>
        public string DependencyId { get; }

        /// <summary>
        /// Gets the name of the dependency.
        /// </summary>
        public string DependencyName { get; }

        /// <summary>
        /// Gets the custom data of the dependency.
        /// </summary>
        public object DependencyData { get; }

        /// <summary>
        /// Gets the name of the dependency target.
        /// </summary>
        public string TargetName { get; }

        /// <summary>
        /// Gets the code of the result of the interaction with the dependency.
        /// </summary>
        public int? ResultCode { get; }

        /// <summary>
        /// Gets the indication whether or not the operation was successful.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Gets the point in time when the interaction with the HTTP dependency was started.
        /// </summary>
        public string StartTime { get; }

        /// <summary>
        /// Gets the duration of the operation.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the context that provides more insights on the dependency that was measured.
        /// </summary>
        public IDictionary<string, object> Context { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            string contextFormatted = $"{{{String.Join("; ", Context.Select(item => $"[{item.Key}, {item.Value}]"))}}}";
            return $"{DependencyType} {DependencyName} {DependencyData} named {TargetName} in {Duration} at {StartTime} " +
                   $"(Successful: {IsSuccessful} - ResultCode: {ResultCode} - Context: {contextFormatted})";
        }
    }
}
