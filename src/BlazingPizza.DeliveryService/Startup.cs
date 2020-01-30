using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Collector.StackExchangeRedis;
using OpenTelemetry.Trace.Configuration;
using StackExchange.Redis;
using Prometheus;

namespace BlazingPizza.DeliveryService
{
    public class Startup
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
            services.AddHostedService<PizzaMaker>();

            var connection = ConnectionMultiplexer.Connect(Configuration.GetServiceHostname("Redis"));
            services.AddSingleton(connection);

            services.AddOpenTelemetry((TracerBuilder b) =>
            {
                b.AddRequestCollector();
                b.UseZipkin(o => 
                {
                    o.ServiceName = "delivery"; 
                    o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
                });

                b.AddCollector(t =>
                {
                    var collector = new StackExchangeRedisCallsCollector(t);
                    connection.RegisterProfiler(collector.GetProfilerSessionsFactory());
                    return collector;
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseHttpMetrics();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();

                endpoints.MapHealthChecks("/healthz");
                endpoints.MapGrpcService<DeliveryServiceImpl>();
            });
        }
    }
}
