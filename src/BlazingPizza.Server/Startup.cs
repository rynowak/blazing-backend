using System;
using System.Linq;
using System.Net.Mime;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace.Configuration;
using Prometheus;

namespace BlazingPizza.Server
{
    public partial class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenTelemetry((TracerBuilder b) =>
            {
                b.AddRequestCollector();
                b.UseZipkin(o => 
                {
                    o.ServiceName = "frontend"; 
                    o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
                });
            });

            RegisterDeliveryGrpcClient(services, Configuration.GetServiceHostname("Delivery", "http://delivery"));
            RegisterMenuGrpcClient(services, Configuration.GetServiceHostname("Menu", "http://menu"));
            RegisterOrdersGrpcClient(services, Configuration.GetServiceHostname("Orders", "http://orders"));

            services.AddHealthChecks();
            services.AddMvc().AddNewtonsoftJson();

            services.AddDbContext<PizzaStoreContext>(options => 
            {
                var filePath = Configuration["Data:Directory"] == null ? "store.db" : $"{Configuration["Data:Directory"]}/store.db";
                options.UseSqlite($"Data Source={filePath}");
            });

            services.AddResponseCompression(options =>
            {
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { MediaTypeNames.Application.Octet });
            });

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "Test Scheme";
                })
                .AddTestAuth(options => {});
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBlazorDebugging();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseHttpMetrics();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseClientSideBlazorFiles<Client.Startup>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapHealthChecks("/healthz");
                
                endpoints.MapControllers();
                endpoints.MapFallbackToClientSideBlazor<Client.Startup>("index.html");
            });
        }
    }
}
