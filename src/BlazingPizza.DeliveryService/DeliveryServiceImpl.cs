using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using StackExchange.Redis;

namespace BlazingPizza.DeliveryService
{
    public class DeliveryServiceImpl : DeliveryService.DeliveryServiceBase
    {
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly ConnectionMultiplexer multiplexer;

        public DeliveryServiceImpl(ConnectionMultiplexer multiplexer)
        {
            this.multiplexer = multiplexer;
        }

        public async override Task<DeliveryStatus> GetDeliveryStatus(DeliveryRequest request, ServerCallContext context)
        {
            var status = new OrderStatus()
            {
                Id = request.OrderId,
                Status = "Preparing",
            };

            var database = multiplexer.GetDatabase();
            var value = await database.StringGetAsync($"orderstatus-{request.OrderId}");
            if (value != RedisValue.Null)
            {
                status = JsonSerializer.Deserialize<OrderStatus>(value.ToString(), options);
            }

            return new DeliveryStatus()
            {
                Status = status.Status,
                Location = new Google.Type.LatLng()
                {
                    Latitude = status.CurrentLocation?.Latitude ?? 0,
                    Longitude = status.CurrentLocation?.Longitude ?? 0,
                }
            };
        }

        public async override Task TrackDelivery(DeliveryRequest request, IServerStreamWriter<DeliveryStatus> responseStream, ServerCallContext context)
        {
            var database = multiplexer.GetDatabase();
            var subscriber = multiplexer.GetSubscriber();

            var channel = await subscriber.SubscribeAsync($"orderupdates-{request.OrderId}");

            try
            {
                var status = new OrderStatus()
                {
                    Id = request.OrderId,
                    Status = "Preparing",
                };

                var value = await database.StringGetAsync($"orderstatus-{request.OrderId}");
                if (value != RedisValue.Null)
                {
                    status = JsonSerializer.Deserialize<OrderStatus>(value.ToString(), options);
                }

                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await responseStream.WriteAsync(new DeliveryStatus()
                    {
                        Status = status.Status,
                        Location = new Google.Type.LatLng()
                        {
                            Latitude = status.CurrentLocation?.Latitude ?? 0,
                            Longitude = status.CurrentLocation?.Longitude ?? 0,
                        }
                    });

                    if (status.Status == "Delivered")
                    {
                        return;
                    }

                    var message = await channel.ReadAsync(context.CancellationToken);
                    status = JsonSerializer.Deserialize<OrderStatus>(message.Message.ToString(), options);
                }
            }
            finally
            {
                channel.Unsubscribe();
            }
        }
    }
}