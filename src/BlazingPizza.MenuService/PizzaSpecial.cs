namespace BlazingPizza
{
    /// <summary>
    /// Represents a pre-configured template for a pizza a user can order
    /// </summary>
    public class PizzaSpecial
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal BasePrice { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public MenuService.PizzaSpecial ToGrpc()
        {
            return new MenuService.PizzaSpecial()
            {
                Id = Id,
                Name = Name,
                Description = Description,
                ImageUrl = ImageUrl,
                BasePrice = new Google.Type.Money()
                {
                    DecimalValue = BasePrice,
                },
            };
        }
    }
}
