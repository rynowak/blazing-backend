using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlazingPizza.OrderService
{
    public partial class Startup
    {
        private void ConfigureDatabase(IServiceCollection services)
        {
            services.AddDbContext<PizzaStoreContext>(options => 
            {
                options.UseSqlServer(Configuration.GetConnectionString("OrdersDatabase"));
            });
        }
    }
}