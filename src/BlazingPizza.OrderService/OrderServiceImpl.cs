using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BlazingPizza.OrderService
{
    public class OrderServiceImpl : OrderService.OrderServiceBase
    {
        private readonly PizzaStoreContext db;

        public OrderServiceImpl(PizzaStoreContext db)
        {
            this.db = db;
        }

        public async override Task<OrderHistoryReply> GetOrderHistory(OrderHistoryRequest request, Grpc.Core.ServerCallContext context)
        {
            var orders = await db.Orders
                .Where(o => o.UserId == request.UserId)
                .Include(o => o.DeliveryLocation)
                .Include(o => o.Pizzas).ThenInclude(p => p.Special)
                .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
                .ToListAsync();

            var reply = new OrderHistoryReply();
            reply.Orders.Add(orders.Select(o => o.ToGrpc()));
            return reply;
        }

        public async override Task<OrderDetailsReply> GetOrderDetails(OrderDetailsRequest request, Grpc.Core.ServerCallContext context)
        {
            var order = await db.Orders
                .Where(o => o.OrderId == request.OrderId)
                .Where(o => o.UserId == request.UserId)
                .Include(o => o.DeliveryLocation)
                .Include(o => o.Pizzas).ThenInclude(p => p.Special)
                .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
                .SingleOrDefaultAsync();

            var reply = new OrderDetailsReply
            {
                Order = order?.ToGrpc()
            };
            return reply;
        }
        
        public async override Task<PlaceOrderReply> PlaceOrder(PlaceOrderRequest request, Grpc.Core.ServerCallContext context)
        {
            // Create DB representation from request
            var order = new BlazingPizza.Order();

            order.CreatedTime = DateTimeOffset.Now;
            order.DeliveryLocation = new LatLong(51.5001, -0.1239);
            order.UserId = request.Order.UserId;

            order.DeliveryAddress = new BlazingPizza.Address()
            {
                Name = request.Order.DeliveryAddress.Name,
                Line1 = request.Order.DeliveryAddress.Line1,
                Line2 = request.Order.DeliveryAddress.Line2,
                City = request.Order.DeliveryAddress.City,
                Region = request.Order.DeliveryAddress.Region,
                PostalCode = request.Order.DeliveryAddress.PostalCode,
            };

            // Enforce existence of Pizza.SpecialId and Topping.ToppingId
            // in the database - prevent the submitter from making up
            // new specials and toppings
            foreach (var orderedPizza in request.Order.Pizzas)
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

            db.Orders.Attach(order);
            await db.SaveChangesAsync();

            return new PlaceOrderReply() { Id = order.OrderId, };
        }
    }
}