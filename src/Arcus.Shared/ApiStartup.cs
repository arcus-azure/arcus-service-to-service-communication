using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Swashbuckle.AspNetCore.Filters;

namespace Arcus.Shared
{
    public class ApiStartup
    {
        private const string ApplicationInsightsInstrumentationKeyName = "ApplicationInsights_InstrumentationKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiStartup"/> class.
        /// </summary>
        public ApiStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration of key/value apeplication properties.
        /// </summary>
        public IConfiguration Configuration { get; }

        protected void ConfigureOpenApiGeneration(string name, string docFileName, IServiceCollection services)
        {
            var openApiInformation = new OpenApiInfo
            {
                Title = name,
                Version = "v1"
            };

            services.AddSwaggerGen(swaggerGenerationOptions =>
            {
                swaggerGenerationOptions.SwaggerDoc("v1", openApiInformation);
                swaggerGenerationOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, docFileName));

                swaggerGenerationOptions.OperationFilter<AddHeaderOperationFilter>("X-Transaction-Id", "Transaction ID is used to correlate multiple operation calls. A new transaction ID will be generated if not specified.",
                    false);
                swaggerGenerationOptions.OperationFilter<AddResponseHeadersFilter>();
            });
        }

        protected void RestrictToJsonContentType(MvcOptions options)
        {
            var allButJsonInputFormatters = options.InputFormatters.Where(formatter => !(formatter is SystemTextJsonInputFormatter));
            foreach (IInputFormatter inputFormatter in allButJsonInputFormatters)
            {
                options.InputFormatters.Remove(inputFormatter);
            }

            // Removing for text/plain, see https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-3.0#special-case-formatters
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
        }

        protected void ConfigureJsonFormatters(MvcOptions options)
        {
            var onlyJsonInputFormatters = options.InputFormatters.OfType<SystemTextJsonInputFormatter>();
            foreach (SystemTextJsonInputFormatter inputFormatter in onlyJsonInputFormatters)
            {
                inputFormatter.SerializerOptions.IgnoreNullValues = true;
                inputFormatter.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }

            var onlyJsonOutputFormatters = options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>();
            foreach (SystemTextJsonOutputFormatter outputFormatter in onlyJsonOutputFormatters)
            {
                outputFormatter.SerializerOptions.IgnoreNullValues = true;
                outputFormatter.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }
        }

        protected void ExposeOpenApiDocs(string name, IApplicationBuilder app)
        {
            app.UseSwagger(swaggerOptions => { swaggerOptions.RouteTemplate = "api/{documentName}/docs.json"; });
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                swaggerUiOptions.SwaggerEndpoint("/api/v1/docs.json", name);
                swaggerUiOptions.RoutePrefix = "api/docs";
                swaggerUiOptions.DocumentTitle = name;
            });
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        protected LoggerConfiguration CreateLoggerConfiguration(string componentName, IServiceProvider serviceProvider)
        {
            var instrumentationKey = Configuration.GetValue<string>(ApplicationInsightsInstrumentationKeyName);

            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithVersion()
                .Enrich.WithComponentName(componentName)
                .Enrich.WithHttpCorrelationInfo(serviceProvider)
                .WriteTo.Console()
                .WriteTo.AzureApplicationInsightsOnSteroids(instrumentationKey);
        }
    }
}
