using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace BlazingPizza.OrderService
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
            var multiplexer = ConnectionMultiplexer.Connect(Configuration["Redis:Service"]);
            services.AddSingleton<ConnectionMultiplexer>(multiplexer);

            services.AddGrpc();
            services.AddDbContext<PizzaStoreContext>(options => 
            {
                var filePath = Configuration["Data:Directory"] == null ? "orders.db" : $"{Configuration["Data:Directory"]}/orders.db";
                options.UseSqlite($"Data Source={filePath}");
            });
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
                endpoints.MapGrpcService<OrderServiceImpl>();
            });
        }
    }
}
