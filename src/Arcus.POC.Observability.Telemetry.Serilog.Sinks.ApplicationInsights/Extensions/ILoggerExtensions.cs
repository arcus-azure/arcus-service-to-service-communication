using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Arcus.Observability.Telemetry.Core;
using Arcus.Observability.Telemetry.Core.Logging;
using GuardNet;
using Microsoft.AspNetCore.Http;
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
        public const string DependencyFormat = "{@" + ContextProperties.DependencyTracking.DependencyLogEntry + "}";
        public const string RequestFormat = "{@" + ContextProperties.RequestTracking.RequestLogEntry + "}";

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

        public static void LogExtendedServiceBusQueueDependency(
            this ILogger logger,
            string queueName,
            bool isSuccessful,
            DependencyMeasurement measurement,
            Dictionary<string, object> context = null,
            string dependencyId = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotNull(measurement, nameof(measurement), "Requires a dependency measurement instance to track the latency of the Azure Service Bus Queue when tracking the Azure Service Bus Queue dependency");

            LogExtendedServiceBusDependency(logger, queueName, dependencyId, isSuccessful, measurement.StartTime, measurement.Elapsed, context: context);
        }

        /// <summary>
        ///     Logs an Azure Service Bus Dependency.
        /// </summary>
        /// <param name="logger">The logger to track the telemetry.</param>
        /// <param name="queueName">Name of the Service Bus queue</param>
        /// <param name="isSuccessful">Indication whether or not the operation was successful</param>
        /// <param name="startTime">Point in time when the interaction with the HTTP dependency was started</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="context">Context that provides more insights on the dependency that was measured</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="duration"/> is a negative time range.</exception>
        public static void LogExtendedServiceBusQueueDependency(
            this ILogger logger,
            string queueName,
            bool isSuccessful,
            DateTimeOffset startTime,
            TimeSpan duration,
            Dictionary<string, object> context = null,
            string dependencyId = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotNullOrWhitespace(queueName, nameof(queueName), "Requires a non-blank Azure Service Bus Queue name to track an Azure Service Bus Queue dependency");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the Azure Service Bus Queue operation");

            LogExtendedServiceBusDependency(logger, queueName, dependencyId, isSuccessful, startTime, duration, ServiceBusEntityType.Queue, context);
        }

        public static void LogExtendedServiceBusDependency(
            this ILogger logger,
            string entityName,
            string dependencyId,
            bool isSuccessful,
            DateTimeOffset startTime,
            TimeSpan duration,
            ServiceBusEntityType entityType = ServiceBusEntityType.Unknown,
            Dictionary<string, object> context = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotNullOrWhitespace(entityName, nameof(entityName), "Requires a non-blank Azure Service Bus entity name to track an Azure Service Bus dependency");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the Azure Service Bus operation");

            context = context ?? new Dictionary<string, object>();
            context[ContextProperties.DependencyTracking.ServiceBus.EntityType] = entityType;

            logger.LogWarning(DependencyFormat, new ExtendedDependencyLogEntry(
                dependencyType: "Azure Service Bus",
                dependencyId: dependencyId,
                dependencyName: null,
                dependencyData: null,
                targetName: entityName,
                duration: duration,
                startTime: startTime,
                resultCode: null,
                isSuccessful: isSuccessful,
                context: context));
        }

        /// <summary>
        ///     Logs an HTTP request
        /// </summary>
        /// <param name="logger">The logger to track the telemetry.</param>
        /// <param name="request">Request that was done</param>
        /// <param name="response">Response that will be sent out</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="context">Context that provides more insights on the HTTP request that was tracked</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/>, <paramref name="request"/>, or <paramref name="response"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="request"/>'s URI is blank,
        ///     the <paramref name="request"/>'s scheme contains whitespace,
        ///     the <paramref name="request"/>'s host contains whitespace,
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="response"/>'s status code is outside the 0-999 inclusively,
        ///     the <paramref name="duration"/> is a negative time range.
        /// </exception>
        public static void LogHttpRequest(
            this ILogger logger,
            HttpRequestMessage request,
            HttpResponseMessage response,
            TimeSpan duration,
            Dictionary<string, object> context = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request instance to track a HTTP request");
            Guard.NotNull(response, nameof(response), "Requires a HTTP response instance to track a HTTP request");
            Guard.NotNull(request.RequestUri, nameof(request.RequestUri), "Requires a request URI to track a HTTP request");
            Guard.For<ArgumentException>(() => request.RequestUri.Scheme?.Contains(" ") == true, "Requires a HTTP request scheme without whitespace");
            Guard.For<ArgumentException>(() => request.RequestUri.Host?.Contains(" ") == true, "Requires a HTTP request host name without whitespace");
            Guard.NotLessThan((int)response.StatusCode, 0, nameof(response), "Requires a HTTP response status code that's within the 0-999 range to track a HTTP request");
            Guard.NotGreaterThan((int)response.StatusCode, 999, nameof(response), "Requires a HTTP response status code that's within the 0-999 range to track a HTTP request");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the request operation");

            LogHttpRequest(logger, request, response.StatusCode, duration, context);
        }

        /// <summary>
        ///     Logs an HTTP request
        /// </summary>
        /// <param name="logger">The logger to track the telemetry.</param>
        /// <param name="request">Request that was done</param>
        /// <param name="responseStatusCode">HTTP status code returned by the service</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="context">Context that provides more insights on the HTTP request that was tracked</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> or <paramref name="request"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="request"/>'s URI is blank,
        ///     the <paramref name="request"/>'s scheme contains whitespace,
        ///     the <paramref name="request"/>'s host contains whitespace,
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="responseStatusCode"/>'s status code is outside the 0-999 inclusively,
        ///     the <paramref name="duration"/> is a negative time range.
        /// </exception>
        public static void LogHttpRequest(
            this ILogger logger,
            HttpRequestMessage request,
            HttpStatusCode responseStatusCode,
            TimeSpan duration,
            Dictionary<string, object> context = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request instance to track a HTTP request");
            Guard.NotNull(request.RequestUri, nameof(request.RequestUri), "Requires a request URI to track a HTTP request");
            Guard.For<ArgumentException>(() => request.RequestUri.Scheme?.Contains(" ") == true, "Requires a HTTP request scheme without whitespace");
            Guard.For<ArgumentException>(() => request.RequestUri.Host?.Contains(" ") == true, "Requires a HTTP request host name without whitespace");
            Guard.NotLessThan((int)responseStatusCode, 0, nameof(responseStatusCode), "Requires a HTTP response status code that's within the 0-999 range to track a HTTP request");
            Guard.NotGreaterThan((int)responseStatusCode, 999, nameof(responseStatusCode), "Requires a HTTP response status code that's within the 0-999 range to track a HTTP request");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the request operation");

            context = context ?? new Dictionary<string, object>();

            var statusCode = (int)responseStatusCode;
            string resourcePath = request.RequestUri.AbsolutePath;
            string host = $"{request.RequestUri.Scheme}://{request.RequestUri.Host}";

            logger.LogWarning(RequestFormat, new ExtendedRequestLogEntry(request.Method.ToString(), host, resourcePath, statusCode, duration, context));
        }
        /// <summary>
        ///     Logs an HTTP request
        /// </summary>
        /// <param name="logger">Logger to use</param>
        /// <param name="request">Request that was done</param>
        /// <param name="response">Response that will be sent out</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="context">Context that provides more insights on the HTTP request that was tracked</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/>, <paramref name="request"/>, or <paramref name="response"/> is <c>null</c></exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="request"/>'s scheme contains whitespace, the <paramref name="request"/>'s host is missing or contains whitespace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="duration"/> is a negative time range.</exception>
        public static void LogHttpRequest(this ILogger logger, HttpRequest request, HttpResponse response, TimeSpan duration, Dictionary<string, object> context = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request instance to track a HTTP request");
            Guard.NotNull(response, nameof(response), "Requires a HTTP response instance to track a HTTP request");
            Guard.For(() => !request.Path.HasValue, new ArgumentException("Requires a HTTP request with a path", nameof(request)));
            Guard.For(() => request.Method is null, new ArgumentException("Requires a HTTP request with a HTTP method", nameof(request)));
            Guard.For(() => request.Scheme?.Contains(" ") == true, new ArgumentException("Requires a HTTP request scheme without whitespace to track a HTTP request", nameof(request)));
            Guard.For(() => !request.Host.HasValue, new ArgumentException("Requires a HTTP request with a host value to track a HTTP request", nameof(request)));
            Guard.For(() => request.Host.ToString()?.Contains(" ") == true, new ArgumentException("Requires a HTTP request host name without whitespace to track a HTTP request", nameof(request)));
            Guard.NotLessThan(response.StatusCode, 0, nameof(response), "Requires a HTTP response status code that's within the 0-999 range to track a HTTP request");
            Guard.NotGreaterThan(response.StatusCode, 999, nameof(response), "Requires a HTTP response status code that's within the 0-999 range to track a HTTP request");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the Azure Blob storage operation");

            LogHttpRequest(logger, request, response.StatusCode, duration, context);
        }

        /// <summary>
        ///     Logs an HTTP request
        /// </summary>
        /// <param name="logger">Logger to use</param>
        /// <param name="request">Request that was done</param>
        /// <param name="responseStatusCode">HTTP status code returned by the service</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="context">Context that provides more insights on the HTTP request that was tracked</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> or <paramref name="request"/> is <c>null</c></exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="request"/>'s scheme contains whitespace, the <paramref name="request"/>'s host is missing or contains whitespace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="duration"/> is a negative time range.</exception>
        public static void LogHttpRequest(this ILogger logger, HttpRequest request, int responseStatusCode, TimeSpan duration, Dictionary<string, object> context = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request instance to track a HTTP request");
            Guard.For(() => !request.Path.HasValue, new ArgumentException("Requires a HTTP request with a path", nameof(request)));
            Guard.For(() => request.Method is null, new ArgumentException("Requires a HTTP request with a HTTP method", nameof(request)));
            Guard.For(() => request.Scheme?.Contains(" ") == true, new ArgumentException("Requires a HTTP request scheme without whitespace to track a HTTP request", nameof(request)));
            Guard.For(() => !request.Host.HasValue, new ArgumentException("Requires a HTTP request with a host value to track a HTTP request", nameof(request)));
            Guard.For(() => request.Host.ToString()?.Contains(" ") == true, new ArgumentException("Requires a HTTP request host name without whitespace to track a HTTP request", nameof(request)));
            Guard.NotLessThan(responseStatusCode, 0, nameof(responseStatusCode), "Requires a HTTP response status code that's within the 0-999 range to track a HTTP request");
            Guard.NotGreaterThan(responseStatusCode, 999, nameof(responseStatusCode), "Requires a HTTP response status code that's within the 0-999 range to track a HTTP request");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the HTTP request");

            context = context ?? new Dictionary<string, object>();

            PathString resourcePath = request.Path;
            var host = $"{request.Scheme}://{request.Host}";

            logger.LogWarning(MessageFormats.RequestFormat, new ExtendedRequestLogEntry(request.Method, host, resourcePath, responseStatusCode, duration, context));
        }

        /// <summary>
        ///     Logs an HTTP request
        /// </summary>
        /// <param name="logger">Logger to use</param>
        /// <param name="request">Request that was done</param>
        /// <param name="responseStatusCode">HTTP status code returned by the service</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="context">Context that provides more insights on the HTTP request that was tracked</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> or <paramref name="request"/> is <c>null</c></exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="request"/>'s scheme contains whitespace, the <paramref name="request"/>'s host is missing or contains whitespace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="duration"/> is a negative time range.</exception>
        public static void LogServiceBusQueueRequest(this ILogger logger, string namespaceEndpoint, string entityName, bool isSuccessful, TimeSpan duration, DateTimeOffset startTime, Dictionary<string, object> context = null, string operationName = null)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track telemetry");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the HTTP request");

            context = context ?? new Dictionary<string, object>();
            operationName = string.IsNullOrWhiteSpace(operationName) ? "Process" : operationName;

            // TODO: Add validation for endpoint as it should be 'sb://{name}.servicebus.windows.net/'
            // Keep in mind that for other clouds the suffix might be different

            context["ServiceBus-Entity"] = entityName;
            context["ServiceBus-Endpoint"] = namespaceEndpoint;

            logger.LogWarning(MessageFormats.RequestFormat, new ExtendedRequestLogEntry(isSuccessful, duration, "Azure Service Bus", operationName, context, startTime));
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
    /// <summary>Represents a HTTP request as a log entry.</summary>
    public class ExtendedRequestLogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Arcus.Observability.Telemetry.Core.Logging.RequestLogEntry" /> class.
        /// </summary>
        /// <param name="method">The HTTP method of the request.</param>
        /// <param name="host">The host that was requested.</param>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="statusCode">The HTTP status code returned by the service.</param>
        /// <param name="duration">The duration of the processing of the request.</param>
        /// <param name="context">The context that provides more insights on the HTTP request that was tracked.</param>
        /// <exception cref="T:System.ArgumentNullException">Thrown when the <paramref name="duration" /> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentException">
        ///     Thrown when the <paramref name="uri" />'s URI is blank,
        ///     the <paramref name="host" /> contains whitespace,
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="statusCode" />'s status code is outside the 100-599 inclusively,
        ///     the <paramref name="duration" /> is a negative time range.
        /// </exception>
        public ExtendedRequestLogEntry(
          bool isSuccessful,
          TimeSpan duration,
          string sourceSystem,
          string sourceName,
          IDictionary<string, object> context,
          DateTimeOffset? requestTime)
        {
            DateTimeOffset decidedRequestTime = requestTime ?? DateTimeOffset.UtcNow;
            this.RequestMethod = "method";
            this.RequestHost = "host";
            this.RequestUri = "uri";
            this.ResponseStatusCode = isSuccessful?200:500;
            this.RequestDuration = duration;
            this.RequestTime = decidedRequestTime.ToString(FormatSpecifiers.InvariantTimestampFormat);
            this.SourceSystem = sourceSystem;
            this.SourceName = sourceName;
            this.Context = context;
            this.Context["TelemetryType"] = (object)TelemetryType.Request;
        }
        public ExtendedRequestLogEntry(
            string method,
            string host,
            string uri,
            int statusCode,
            TimeSpan duration,
            IDictionary<string, object> context)
        {
            Guard.For<ArgumentException>(() => host?.Contains(" ") == true, "Requires a HTTP request host name without whitespace");
            Guard.NotLessThan(statusCode, 100, nameof(statusCode), "Requires a HTTP response status code that's within the 100-599 range to track a HTTP request");
            Guard.NotGreaterThan(statusCode, 599, nameof(statusCode), "Requires a HTTP response status code that's within the 100-599 range to track a HTTP request");
            Guard.NotLessThan(duration, TimeSpan.Zero, nameof(duration), "Requires a positive time duration of the request operation");

            RequestMethod = method;
            RequestHost = host;
            RequestUri = uri;
            ResponseStatusCode = statusCode;
            RequestDuration = duration;
            RequestTime = DateTimeOffset.UtcNow.ToString(FormatSpecifiers.InvariantTimestampFormat);
            Context = context;
            SourceSystem = "HTTP";
            SourceName = $"{method}{uri}";
            Context[ContextProperties.General.TelemetryType] = TelemetryType.Request;
        }

        /// <summary>Gets the HTTP method of the request.</summary>
        public string RequestMethod { get; set; }

        /// <summary>Gets the host that was requested.</summary>
        public string RequestHost { get; set; }

        /// <summary>Gets ths URI of the request.</summary>
        public string RequestUri { get; set; }

        /// <summary>
        /// Gets the HTTP response status code that was returned by the service.
        /// </summary>
        public int ResponseStatusCode { get; set; }

        /// <summary>Gets the duration of the processing of the request.</summary>
        public TimeSpan RequestDuration { get; set; }

        /// <summary>Gets the date when the request occurred.</summary>
        public string RequestTime { get; set; }

        /// <summary>Gets the date when the request occurred.</summary>
        public string SourceSystem { get; set; }

        /// <summary>Gets the date when the request occurred.</summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Gets the context that provides more insights on the HTTP request that was tracked.
        /// </summary>
        public IDictionary<string, object> Context { get; set; }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => string.Format("{0} {1}/{2} completed with {3} in {4} at {5} - (Context: {6})", (object)this.RequestMethod, (object)this.RequestHost, (object)this.RequestUri, (object)this.ResponseStatusCode, (object)this.RequestDuration, (object)this.RequestTime, (object)("{" + string.Join("; ", this.Context.Select<KeyValuePair<string, object>, string>((Func<KeyValuePair<string, object>, string>)(item => string.Format("[{0}, {1}]", (object)item.Key, item.Value)))) + "}"));
    }
}
