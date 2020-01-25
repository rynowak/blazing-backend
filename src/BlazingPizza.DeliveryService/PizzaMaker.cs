using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BlazingPizza.DeliveryService
{
    internal class PizzaMaker : BackgroundService
    {
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly ConnectionMultiplexer multiplexer;
        private readonly ILogger<PizzaMaker> logger;

        public PizzaMaker(ConnectionMultiplexer multiplexer, ILogger<PizzaMaker> logger)
        {
            this.multiplexer = multiplexer;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var database = multiplexer.GetDatabase();
            var subscriber = multiplexer.GetSubscriber();
            try
            {
                var channel = await subscriber.SubscribeAsync("neworder");
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (true)
                    {
                        // Drain existing work
                        var value = await database.ListLeftPopAsync("orders");
                        if (!TryProcessWork(value))
                        {
                            logger.LogInformation("Finished orders. Going to sleep.");
                            break;
                        }
                    }

                    // Wait for notification about new work.
                    await channel.ReadAsync(stoppingToken);
                    logger.LogInformation("There's work to do! Waking up.");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PizzaMaker Crashed!");
            }
        }

        private bool TryProcessWork(RedisValue value)
        {
            if (value == RedisValue.Null)
            {
                return false;
            }

            var order = JsonSerializer.Deserialize<Order>(value.ToString(), options);
            logger.LogInformation("Got order {OrderId} from queue.", order.OrderId);

            // Offload processing so we can take the next order, making a pizza takes a little while.
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessAsync(order);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Encountered error processing order {OrderId}.", order.OrderId);
                }
            });

            return true;
        }

        private async Task ProcessAsync(Order order)
        {
            var database = multiplexer.GetDatabase();
            var subscriber = multiplexer.GetSubscriber();
            var status = new OrderStatus()
            {
                Id = order.OrderId,
                Status = "Preparing",
                CurrentLocation = null,
            };

            await database.StringSetAsync($"orderstatus-{order.OrderId}", JsonSerializer.Serialize(status, options));
            await subscriber.PublishAsync($"orderupdates-{order.OrderId}", JsonSerializer.Serialize(status, options));

            await Task.Delay(TimeSpan.FromSeconds(10));

            var startPosition = ComputeStartPosition(order);
            status.Status = "Out for delivery";
            status.CurrentLocation = startPosition;

            await database.StringSetAsync($"orderstatus-{order.OrderId}", JsonSerializer.Serialize(status, options));
            await subscriber.PublishAsync($"orderupdates-{order.OrderId}", JsonSerializer.Serialize(status, options));

            var stopwatch = Stopwatch.StartNew();
            var duration = TimeSpan.FromMinutes(1);
            while (stopwatch.Elapsed < duration)
            {
                var proportionOfDeliveryCompleted = Math.Min(1, stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                status.CurrentLocation = LatLong.Interpolate(startPosition, order.DeliveryLocation, proportionOfDeliveryCompleted);

                await database.StringSetAsync($"orderstatus-{order.OrderId}", JsonSerializer.Serialize(status, options));
                await subscriber.PublishAsync($"orderupdates-{order.OrderId}", JsonSerializer.Serialize(status, options));

                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            status.Status = "Delivered";
            await database.StringSetAsync($"orderstatus-{order.OrderId}", JsonSerializer.Serialize(status, options));
            await subscriber.PublishAsync($"orderupdates-{order.OrderId}", JsonSerializer.Serialize(status, options));

            logger.LogInformation("Delivered order {OrderId}.", order.OrderId);
        }

        private static LatLong ComputeStartPosition(Order order)
        {
            // Random but deterministic based on order ID
            var rng = new Random(order.OrderId);
            var distance = 0.01 + rng.NextDouble() * 0.02;
            var angle = rng.NextDouble() * Math.PI * 2;
            var offset = (distance * Math.Cos(angle), distance * Math.Sin(angle));
            return new LatLong(order.DeliveryLocation.Latitude + offset.Item1, order.DeliveryLocation.Longitude + offset.Item2);
        }
    }
}