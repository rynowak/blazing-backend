using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlazingPizza.ComponentsLibrary.Map;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace BlazingPizza.Server
{
    [Route("orders")]
    [ApiController]
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly PizzaStoreContext _db;
        private readonly OrderService.OrderService.OrderServiceClient orders;

        public OrdersController(PizzaStoreContext db, OrderService.OrderService.OrderServiceClient orders)
        {
            _db = db;
            this.orders = orders;
        }

        [HttpGet]
        public async Task<ActionResult<List<OrderWithStatus>>> GetOrders()
        {
            var reply = await orders.GetOrderHistoryAsync(new OrderService.OrderHistoryRequest()
            {
                UserId = GetUserId(),
            });

            return reply.Orders.OrderByDescending(o => o.CreatedTime).Select(o => OrderWithStatus.FromOrder(FromGrpc(o))).ToList();
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<OrderWithStatus>> GetOrderWithStatus(int orderId)
        {
            var reply = await orders.GetOrderDetailsAsync(new OrderService.OrderDetailsRequest()
            {
                OrderId = orderId,
                UserId = GetUserId(),
            });

            if (reply.Order == null)
            {
                return NotFound();
            }

            return OrderWithStatus.FromOrder(FromGrpc(reply.Order));
        }

        [HttpPost]
        public async Task<ActionResult<int>> PlaceOrder(Order order)
        {
            order.UserId = GetUserId();

            var reply = await orders.PlaceOrderAsync(new OrderService.PlaceOrderRequest()
            {
                Order = ToGrpc(order),
            });
            var orderId = reply.Id;

            // In the background, send push notifications if possible
            var subscription = await _db.NotificationSubscriptions.Where(e => e.UserId == GetUserId()).SingleOrDefaultAsync();
            if (subscription != null)
            {
                _ = TrackAndSendNotificationsAsync(orderId, subscription);
            }

            return orderId;
        }

        private string GetUserId()
        {
            // This will be the user's twitter username
            return HttpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
        }

        private static async Task TrackAndSendNotificationsAsync(int orderId, NotificationSubscription subscription)
        {
            // In a realistic case, some other backend process would track
            // order delivery progress and send us notifications when it
            // changes. Since we don't have any such process here, fake it.
            await Task.Delay(OrderWithStatus.PreparationDuration);
            await SendNotificationAsync(orderId, subscription, "Your order has been dispatched!");

            await Task.Delay(OrderWithStatus.DeliveryDuration);
            await SendNotificationAsync(orderId, subscription, "Your order is now delivered. Enjoy!");
        }

        private static async Task SendNotificationAsync(int orderId, NotificationSubscription subscription, string message)
        {
            // For a real application, generate your own
            var publicKey = "BLC8GOevpcpjQiLkO7JmVClQjycvTCYWm6Cq_a7wJZlstGTVZvwGFFHMYfXt6Njyvgx_GlXJeo5cSiZ1y4JOx1o";
            var privateKey = "OrubzSz3yWACscZXjFQrrtDwCKg-TGFuWhluQ2wLXDo";

            var pushSubscription = new PushSubscription(subscription.Url, subscription.P256dh, subscription.Auth);
            var vapidDetails = new VapidDetails("mailto:<someone@example.com>", publicKey, privateKey);
            var webPushClient = new WebPushClient();
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    message,
                    url = $"myorders/{orderId}",
                });
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error sending push notification: " + ex.Message);
            }
        }

        private static OrderService.Order ToGrpc(Order order)
        {
            var grpc = new OrderService.Order();
            grpc.DeliveryAddress = new OrderService.Address()
            {
                Name = order.DeliveryAddress.Name,
                Line1 = order.DeliveryAddress.Line1,
                Line2 = order.DeliveryAddress.Line2,
                City = order.DeliveryAddress.City,
                Region = order.DeliveryAddress.Region,
                PostalCode = order.DeliveryAddress.PostalCode,
            };
            grpc.UserId = order.UserId;
            foreach (var pizza in order.Pizzas)
            {
                var grpcPizza = new OrderService.Pizza();
                grpcPizza.Size = pizza.Size;
                grpcPizza.Special = new OrderService.PizzaSpecial()
                {
                    BasePrice = new Google.Type.Money()
                    {
                        DecimalValue = pizza.Special.BasePrice,
                    },
                    Name = pizza.Special.Name,
                    Description = pizza.Special.Description,
                    ImageUrl = pizza.Special.ImageUrl,
                };

                foreach (var topping in pizza.Toppings)
                {
                    var grpcTopping = new OrderService.Topping();
                    grpcTopping.Name = topping.Topping.Name;
                    grpcTopping.Price = new Google.Type.Money()
                    {
                        DecimalValue = topping.Topping.Price,
                    };
                    grpcPizza.Toppings.Add(grpcTopping);
                }

                grpc.Pizzas.Add(grpcPizza);
            }

            return grpc;
        }

        private static Order FromGrpc(OrderService.Order grpc)
        {
            var order = new BlazingPizza.Order();
            order.OrderId = grpc.Id;

            order.CreatedTime = grpc.CreatedTime.ToDateTimeOffset();
            order.UserId = grpc.UserId;

            order.DeliveryLocation = new LatLong()
            {
                Latitude = grpc.DeliveryLocation.Latitude,
                Longitude = grpc.DeliveryLocation.Longitude,
            };

            order.DeliveryAddress = new BlazingPizza.Address()
            {
                Name = grpc.DeliveryAddress?.Name,
                Line1 = grpc.DeliveryAddress?.Line1,
                Line2 = grpc.DeliveryAddress?.Line2,
                City = grpc.DeliveryAddress?.City,
                Region = grpc.DeliveryAddress?.Region,
                PostalCode = grpc.DeliveryAddress?.PostalCode,
            };

            // Enforce existence of Pizza.SpecialId and Topping.ToppingId
            // in the database - prevent the submitter from making up
            // new specials and toppings
            foreach (var orderedPizza in grpc.Pizzas)
            {
                var special = new BlazingPizza.PizzaSpecial()
                {
                    BasePrice = orderedPizza.Special.BasePrice.DecimalValue,
                    Name = orderedPizza.Special.Name,
                    Description = orderedPizza.Special.Description,
                    ImageUrl = orderedPizza.Special.ImageUrl,
                };

                var pizza = new BlazingPizza.Pizza()
                {
                    Size = orderedPizza.Size,
                    Special = special,
                };

                foreach (var orderedTopping in orderedPizza.Toppings)
                {
                    var topping = new PizzaTopping()
                    {
                        Topping = new BlazingPizza.Topping()
                        {
                            Name = orderedTopping.Name,
                            Price = orderedTopping.Price.DecimalValue,
                        },
                    };

                    pizza.Toppings.Add(topping);
                }

                order.Pizzas.Add(pizza);
            }

            return order;
        }
    }
}
