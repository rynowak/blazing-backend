using Microsoft.EntityFrameworkCore;

namespace BlazingPizza
{
    public class PizzaStoreContext : DbContext
    {
        public PizzaStoreContext()
        {
        }

        public PizzaStoreContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<PizzaSpecial> Specials { get; set; }

        public DbSet<Topping> Toppings { get; set; }
    }
}
