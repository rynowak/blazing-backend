using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BlazingPizza.MenuService
{
    public class MenuServiceImpl : MenuService.MenuServiceBase
    {
        private readonly PizzaStoreContext _db;

        public MenuServiceImpl(PizzaStoreContext db)
        {
            _db = db;
        }

        public async override Task<ToppingReply> GetToppings(ToppingRequest request, Grpc.Core.ServerCallContext context)
        {
            var toppings = await _db.Toppings.ToListAsync();
            var reply = new ToppingReply();
            reply.Toppings.Add(toppings.Select(t => t.ToGrpc()));
            return reply;
        }

        public async override Task<PizzaSpecialReply> GetPizzaSpecials(PizzaSpecialRequest request, Grpc.Core.ServerCallContext context)
        {
            var specials = await _db.Specials.ToListAsync();
            var reply = new PizzaSpecialReply();
            reply.Specials.Add(specials.Select(s => s.ToGrpc()));
            return reply;
        }
    }
}