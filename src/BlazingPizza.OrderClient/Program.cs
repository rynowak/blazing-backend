using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace BlazingPizza.OrderClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("usage: BlazingPizza.OrderClient <url>");
                return;
            }

            var uri = args[0];
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions()
            {
                Credentials = ChannelCredentials.Insecure,
            });
            var client = new OrderService.OrderService.OrderServiceClient(channel);

            await client.PlaceOrderAsync(new OrderService.PlaceOrderRequest()
            {
                Order = new OrderService.Order()
                {
                    UserId = "test",
                    DeliveryLocation = new Google.Type.LatLng()
                    {
                        Latitude = 51.5001,
                        Longitude = -0.1239,
                    },
                    DeliveryAddress = new OrderService.Address()
                    {
                        Name = "test",
                        Line1 = "text",
                        Line2 = "test",
                        City = "test",
                        Region = "test",
                        PostalCode = "test",
                    },
                    Pizzas  =
                    {
                        new OrderService.Pizza()
                        {
                            Size = 12,
                            Special = new OrderService.PizzaSpecial()
                            {
                                Name = "test",
                                Description = "test",
                                ImageUrl = "test",
                                BasePrice = new Google.Type.Money()
                                {
                                    DecimalValue = 1.0m,
                                },
                            },
                            Toppings =
                            {
                                new OrderService.Topping()
                                {
                                    Name = "text",
                                    Price = new Google.Type.Money()
                                    {
                                        DecimalValue = 1.0m,
                                    }
                                },
                            },
                        },
                    },
                },
            });
        }
    }
}
