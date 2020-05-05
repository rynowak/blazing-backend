using System;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            RegisterDeliveryGrpcClient(services, Configuration.GetServiceUri("delivery"));
            RegisterMenuGrpcClient(services, Configuration.GetServiceUri("menu"));
            RegisterOrdersGrpcClient(services, Configuration.GetServiceUri("orders"));

            services.AddHealthChecks();
            services.AddMvc().AddNewtonsoftJson();

            services.AddDbContext<PizzaStoreContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("PizzaDatabase"), c => c.EnableRetryOnFailure(30));
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
                .AddTestAuth(options => { });
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

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseClientSideBlazorFiles<Client.Startup>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");

                endpoints.MapControllers();
                endpoints.MapFallbackToClientSideBlazor<Client.Startup>("index.html");
            });
        }
    }
}
