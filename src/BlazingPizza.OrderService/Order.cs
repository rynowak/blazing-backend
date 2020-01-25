using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

namespace BlazingPizza
{
    public class Order
    {
        public int OrderId { get; set; }

        public string UserId { get; set; }

        public DateTimeOffset CreatedTime { get; set; }

        public Address DeliveryAddress { get; set; } = new Address();

        public LatLong DeliveryLocation { get; set; }

        public List<Pizza> Pizzas { get; set; } = new List<Pizza>();

        public decimal GetTotalPrice() => Pizzas.Sum(p => p.GetTotalPrice());

        public string GetFormattedTotalPrice() => GetTotalPrice().ToString("0.00");

        public OrderService.Order ToGrpc()
        {
            var order = new OrderService.Order();
            order.Id = OrderId;
            order.CreatedTime = Timestamp.FromDateTimeOffset(CreatedTime);
            if (order.DeliveryAddress?.Name != null)
            {
                order.DeliveryAddress = DeliveryAddress?.ToGrpc();
            }
            order.DeliveryLocation = new Google.Type.LatLng()
            {
                Latitude = DeliveryLocation.Latitude,
                Longitude = DeliveryLocation.Longitude,
            };
            order.Pizzas.Add(Pizzas.Select(p => p.ToGrpc()));
            return order;
        }
    }
}
