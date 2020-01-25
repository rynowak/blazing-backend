namespace BlazingPizza
{
    public class Topping
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public static Topping FromGrpc(MenuService.Topping topping)
        {
            return new Topping()
            {
                Id = topping.Id,
                Name = topping.Name,
                Price = topping.Price.DecimalValue,
            };
        }

        public MenuService.Topping ToGrpc()
        {
            return new MenuService.Topping()
            {
                Id = Id,
                Name = Name,
                Price = new Google.Type.Money()
                {
                    DecimalValue = Price,
                },
            };
        }
    }
}
