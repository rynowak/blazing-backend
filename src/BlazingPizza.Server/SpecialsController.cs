using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BlazingPizza.Server
{
    [Route("specials")]
    [ApiController]
    public class SpecialsController : Controller
    {
        private readonly MenuService.MenuService.MenuServiceClient menu;

        public SpecialsController(MenuService.MenuService.MenuServiceClient menu)
        {
            this.menu = menu;
        }

        [HttpGet]
        public async Task<ActionResult<List<PizzaSpecial>>> GetSpecials()
        {
            var reply = await menu.GetPizzaSpecialsAsync(new MenuService.PizzaSpecialRequest());
            return reply.Specials.Select(s => FromGrpc(s)).OrderByDescending(s => s.BasePrice).ToList();
        }

        private static PizzaSpecial FromGrpc(MenuService.PizzaSpecial special)
        {
            return new PizzaSpecial()
            {
                Id = special.Id,
                Name = special.Name,
                Description = special.Description,
                ImageUrl = special.ImageUrl,
                BasePrice = special.BasePrice.DecimalValue,
            };
        }
    }
}
