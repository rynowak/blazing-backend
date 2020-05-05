using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace BlazingPizza.OrderService
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
            services.AddGrpc();
            services.AddHealthChecks();

            ConnectionMultiplexer connection = null;
            while(connection is null)
            {
                try
                {
                    connection = ConnectionMultiplexer.Connect(Configuration.GetConnectionString("redis"));
                    services.AddSingleton<ConnectionMultiplexer>(connection);
                }
                catch(Exception)
                {
                }
            }

            ConfigureDatabase(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapGrpcService<OrderServiceImpl>();
            });
        }
    }
}
