using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BlazingPizza.Server
{
    [Route("toppings")]
    [ApiController]
    public class ToppingsController : Controller
    {
        private readonly MenuService.MenuService.MenuServiceClient menu;

        public ToppingsController(MenuService.MenuService.MenuServiceClient menu)
        {
            this.menu = menu;
        }

        [HttpGet]
        public async Task<ActionResult<List<Topping>>> GetToppings()
        {
            var reply = await menu.GetToppingsAsync(new MenuService.ToppingRequest());
            return reply.Toppings.Select(t => FromGrpc(t)).OrderBy(t => t.Name).ToList();
        }

        private static Topping FromGrpc(MenuService.Topping topping)
        {
            return new Topping()
            {
                Id = topping.Id,
                Name = topping.Name,
                Price = topping.Price.DecimalValue,
            };
        }
    }
}
