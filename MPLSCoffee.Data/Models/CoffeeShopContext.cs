using Microsoft.EntityFrameworkCore;
using MPLSCoffee.Data.Models;

namespace MPLSCoffee.Data
{
    public class CoffeeShopContext : DbContext
    {
        public CoffeeShopContext(DbContextOptions<CoffeeShopContext> options)
            : base(options)
        {
        }

        public DbSet<CoffeeShop> CoffeeShops { get; set; }
        public DbSet<CoffeeShopHours> CoffeeShopHours { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CoffeeShopHours>()
                .HasOne(h => h.CoffeeShop)
                .WithMany(s => s.Hours)
                .HasForeignKey(h => h.CoffeeShopPlaceId);
        }
    }
}
