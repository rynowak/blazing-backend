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

namespace BlazingPizza.Server
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
            services.AddSingleton<DeliveryService.DeliveryService.DeliveryServiceClient>(s =>
            {
                var uri = Configuration["Delivery:Service"] ?? "http://delivery";
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                var channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions()
                {
                    Credentials = ChannelCredentials.Insecure,
                });
                return new DeliveryService.DeliveryService.DeliveryServiceClient(channel);
            });

            services.AddSingleton<MenuService.MenuService.MenuServiceClient>(s =>
            {
                var uri = Configuration["Menu:Service"] ?? "http://menu";
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                var channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions()
                {
                    Credentials = ChannelCredentials.Insecure,
                });
                return new MenuService.MenuService.MenuServiceClient(channel);
            });

            services.AddSingleton<OrderService.OrderService.OrderServiceClient>(s =>
            {
                var uri = Configuration["Orders:Service"] ?? "http://orders";
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                var channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions()
                {
                    Credentials = ChannelCredentials.Insecure,
                });
                return new OrderService.OrderService.OrderServiceClient(channel);
            });

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
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddTwitter(twitterOptions =>
                {
                    twitterOptions.ConsumerKey = Configuration["Authentication:Twitter:ConsumerKey"];
                    twitterOptions.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];
                    twitterOptions.Events.OnRemoteFailure = (context) =>
                    {
                        context.HandleResponse();
                        return context.Response.WriteAsync("<script>window.close();</script>");
                    };
                });
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
                endpoints.MapControllers();
                endpoints.MapFallbackToClientSideBlazor<Client.Startup>("index.html");
            });
        }
    }
}
