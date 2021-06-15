using Arcus.API.Salads.Repositories;
using Arcus.API.Salads.Repositories.Interfaces;
using Arcus.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Arcus.API.Salads
{
    public class Startup : ApiStartup
    {
        private const string ComponentName = "Salad API";
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
            
            services.AddScoped<ISaladRepository, SaladRepository>();

            ConfigureOpenApiGeneration(ApiName, "Arcus.API.Salads.Open-Api.xml", services);
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
