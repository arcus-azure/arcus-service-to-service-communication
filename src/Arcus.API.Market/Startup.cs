using Arcus.API.Market.Repositories;
using Arcus.API.Market.Repositories.Interfaces;
using Arcus.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.API.Market
{
    public class Startup : ApiStartup
    {
        private const string ComponentName = "Market API";
        private string ApiName => $"Arcus - {ComponentName}";
        
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsights(ComponentName);

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

            services.AddBaconApiIntegration();
            services.AddScoped<QueueClient>(serviceProvider =>
            {
                var serviceBusConnectionString = Configuration["SERVICEBUS_CONNECTIONSTRING"];
                return new QueueClient(serviceBusConnectionString, "orders");
            });
            services.AddScoped<IOrderRepository, OrderRepository>();

            ConfigureOpenApiGeneration(ApiName, "Arcus.API.Market.Open-Api.xml", services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            
            ExposeOpenApiDocs(ApiName, app);
        }
    }
}
