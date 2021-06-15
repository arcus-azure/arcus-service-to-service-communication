using Arcus.API.Market.Repositories;
using Arcus.API.Market.Repositories.Interfaces;
using Arcus.API.Market.Services;
using Arcus.API.Market.Services.Interfaces;
using Arcus.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
            services.AddHttpCorrelationFromPoc();

            services.AddHttpClient();
            services.AddScoped<IBaconService, BaconService>();
            services.AddScoped<IMarketRepository, MarketRepository>();

            ConfigureOpenApiGeneration(ApiName, "Arcus.API.Market.Open-Api.xml", services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandling();
            app.UseHttpCorrelationFromPoc();
            app.UseRouting();
            app.UseRequestTracking();
            
            ExposeOpenApiDocs(ApiName, app);

            Log.Logger = CreateLoggerConfiguration(ComponentName, app.ApplicationServices).CreateLogger();
        }
    }
}
