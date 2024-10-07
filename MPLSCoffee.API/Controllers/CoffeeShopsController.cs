using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using MPLSCoffee.Data;
using MPLSCoffee.Data.Models;

namespace MPLSCoffee.API.Controllers
{
    public class CoffeeShopsController : ODataController
    {
        private readonly CoffeeShopContext _context;

        public CoffeeShopsController(CoffeeShopContext context)
        {
            _context = context;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_context.CoffeeShops);
        }

        [EnableQuery]
        public IActionResult Get(string key)
        {
            var coffeeShop = _context.CoffeeShops.Find(key);
            if (coffeeShop == null)
            {
                return NotFound();
            }
            return Ok(coffeeShop);
        }
    }
}
