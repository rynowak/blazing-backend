namespace BlazingPizza
{
    public class PizzaTopping
    {
        public Topping Topping { get; set; }

        public int ToppingId { get; set; }
        
        public int PizzaId { get; set; }

        public OrderService.Topping ToGrpc()
        {
            return new OrderService.Topping()
            {
                Name = Topping.Name,
                Price = new Google.Type.Money()
                {
                    DecimalValue = Topping.Price,
                },
            };
        }
    }
}
