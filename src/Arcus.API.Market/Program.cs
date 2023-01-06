using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Arcus.API.Market.Repositories;
using Arcus.API.Market.Repositories.Interfaces;
using Arcus.Security.Core;
using Arcus.Shared.ExampleProviders;
using Arcus.Shared.Services;
using Arcus.Shared.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Swashbuckle.AspNetCore.Filters;

namespace Arcus.API.Market
{
    public class Program
    {
        private const string ApplicationInsightsConnectionStringKeyName = "APPLICATIONINSIGHTS_CONNECTION_STRING";

        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                WebApplication app = CreateWebApplication(args);
                await ConfigureSerilogAsync(app);
                await app.RunAsync();

                return 0;
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static WebApplication CreateWebApplication(string[] args)
        {
            IConfiguration configuration = CreateConfiguration(args);
            WebApplicationBuilder builder = CreateWebApplicationBuilder(args, configuration);
            
            WebApplication app = builder.Build();
            ConfigureApp(app);
            
            return app;
        }

        private static IConfiguration CreateConfiguration(string[] args)
        {
            IConfigurationRoot configuration =
                new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .AddEnvironmentVariables()
                    .Build();

            return configuration;
        }

        private static WebApplicationBuilder CreateWebApplicationBuilder(string[] args, IConfiguration configuration)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            
            builder.Configuration.AddConfiguration(configuration);
            ConfigureServices(builder, configuration);
            ConfigureHost(builder, configuration);
            
            return builder;
        }

        private static void ConfigureServices(WebApplicationBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
            });
            builder.Services.AddControllers(options =>
            {
                options.ReturnHttpNotAcceptable = true;
                options.RespectBrowserAcceptHeader = true;
                options.OnlyAllowJsonFormatting();
                options.ConfigureJsonFormatting(json =>
                {
                    json.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    json.Converters.Add(new JsonStringEnumConverter());
                });
            });

            builder.Services.AddHealthChecks();
            builder.Services.AddHttpCorrelation();

            builder.Services.AddAppName("Market API");
            builder.Services.AddAssemblyAppVersion<Program>();

            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IBaconService, BaconService>();

            builder.Services.AddHttpClient("Bacon API");
            builder.Services.AddAzureClients(options => 
            {
                var serviceBusConnectionString = configuration["SERVICEBUS_CONNECTIONSTRING"];
                options.AddServiceBusClient(connectionString: serviceBusConnectionString);
            });

            ConfigureOpenApi(builder);
        }

       private static void ConfigureOpenApi(WebApplicationBuilder builder)
        {
            var openApiInformation = new OpenApiInfo
            {
                Title = "Arcus.API.Market",
                Version = "v1"
            };

            builder.Services.AddSwaggerGen(swaggerGenerationOptions =>
            {
                swaggerGenerationOptions.SwaggerDoc("v1", openApiInformation);
                swaggerGenerationOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Arcus.API.Market.Open-Api.xml"));

                swaggerGenerationOptions.ExampleFilters();
                swaggerGenerationOptions.OperationFilter<AddHeaderOperationFilter>("X-Transaction-ID", "Transaction ID is used to correlate multiple operation calls. A new transaction ID will be generated if not specified.", false);
                swaggerGenerationOptions.OperationFilter<AddResponseHeadersFilter>();
            });

            builder.Services.AddSwaggerExamplesFromAssemblyOf<HealthReportResponseExampleProvider>();
        }

        private static void ConfigureHost(WebApplicationBuilder builder, IConfiguration configuration)
        {
            string httpEndpointUrl = "http://+:" + configuration.GetValue<int>("ARCUS_HTTP_PORT");
            builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false)
                   .UseUrls(httpEndpointUrl);
            
            builder.Host.ConfigureSecretStore((context, config, stores) =>
            {
                stores.AddConfiguration(config);
            });

            builder.Host.UseSerilog(Log.Logger);
        }

        private static async Task ConfigureSerilogAsync(WebApplication app)
        {
            var secretProvider = app.Services.GetRequiredService<ISecretProvider>();
            //string connectionString = await secretProvider.GetRawSecretAsync("APPINSIGHTS_INSTRUMENTATIONKEY");
            string connectionString = app.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];

            var reloadLogger = (ReloadableLogger) Log.Logger;
            reloadLogger.Reload(config =>
            {
                config.ReadFrom.Configuration(app.Configuration)
                      .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                      .Enrich.FromLogContext()
                      .Enrich.WithVersion(app.Services)
                      .Enrich.WithComponentName(app.Services)
                      .Enrich.WithHttpCorrelationInfo(app.Services)
                      .WriteTo.Console();
            
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    config.WriteTo.AzureApplicationInsightsWithConnectionString(app.Services, "InstrumentationKey=" + connectionString);
                }
                
                return config;
            });
        }

        private static void ConfigureApp(IApplicationBuilder app)
        {
            app.UseHttpCorrelation();
            app.UseRouting();
            app.UseRequestTracking(options =>
            {
                options.OmittedRoutes.Add("/");
                options.OmittedRoutes.Add("/api/docs");
                options.OmittedRoutes.Add("/api/v1/docs.json"); 
            });
            app.UseExceptionHandling();
            
            app.UseSwagger(swaggerOptions =>
            {
                swaggerOptions.RouteTemplate = "api/{documentName}/docs.json";
            });
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                swaggerUiOptions.SwaggerEndpoint("/api/v1/docs.json", "Arcus.API.Market");
                swaggerUiOptions.RoutePrefix = "api/docs";
                swaggerUiOptions.DocumentTitle = "Arcus.API.Market";
            });

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
