using System;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BlazingPizza.PizzaManager
{
    public partial class Startup
    {
        private void RegisterDeliveryGrpcClient(IServiceCollection services, string uri)
        {
            services.AddSingleton<DeliveryService.DeliveryService.DeliveryServiceClient>(s =>
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                var channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions()
                {
                    Credentials = ChannelCredentials.Insecure,
                });

                return new DeliveryService.DeliveryService.DeliveryServiceClient(channel);
            });
        }

        private void RegisterOrdersGrpcClient(IServiceCollection services, string uri)
        {
            services.AddSingleton<OrderService.OrderService.OrderServiceClient>(s =>
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                var channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions()
                {
                    Credentials = ChannelCredentials.Insecure,
                });
                return new OrderService.OrderService.OrderServiceClient(channel);
            });
        }
    }
}