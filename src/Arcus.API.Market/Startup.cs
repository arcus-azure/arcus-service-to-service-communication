using Arcus.API.Market.Repositories;
using Arcus.API.Market.Repositories.Interfaces;
using Arcus.Shared;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.API.Market
{
    public class Startup : ApiStartup
    {
        public const string ComponentName = "Market API";
        private string ApiName => $"Arcus - {ComponentName}";
        
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
            });
            services.AddControllers(options => 
            {
                options.ReturnHttpNotAcceptable = true;
                options.RespectBrowserAcceptHeader = true;

                RestrictToJsonContentType(options);
                ConfigureJsonFormatters(options);
            });

            services.AddHealthChecks();
            services.AddHttpCorrelation(options => options.UpstreamService.ExtractFromRequest = true);

            services.AddBaconApiIntegration();
            services.AddAzureClients(options => 
            {
                var serviceBusConnectionString = Configuration["SERVICEBUS_CONNECTIONSTRING"];
                options.AddServiceBusClient(serviceBusConnectionString);
            });
            services.AddScoped<IOrderRepository, OrderRepository>();

            ConfigureOpenApiGeneration(ApiName, "Arcus.API.Market.Open-Api.xml", services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandling();
            app.UseHttpCorrelation();
            app.UseRouting();
            app.UseRequestTracking();

            ExposeOpenApiDocs(ApiName, app);
        }
    }
}
