using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazingPizza.OrderService
{
    public partial class Startup
    {
        private void ConfigureDatabase(IServiceCollection services)
        {
            services.AddDbContext<PizzaStoreContext>(options => 
            {
                var filePath = Configuration["Data:Directory"] == null ? "orders.db" : $"{Configuration["Data:Directory"]}/orders.db";
                options.UseSqlite($"Data Source={filePath}");
            });
        }
    }
}