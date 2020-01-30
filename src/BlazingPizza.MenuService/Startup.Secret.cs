using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazingPizza.MenuService
{
    public partial class Startup
    {
        private void ConfigureDatabase(IServiceCollection services)
        {
            services.AddDbContext<PizzaStoreContext>(options => 
            {
                var filePath = Configuration["Data:Directory"] == null ? "menu.db" : $"{Configuration["Data:Directory"]}/menu.db";
                options.UseSqlite($"Data Source={filePath}");
            });
        }
    }
}