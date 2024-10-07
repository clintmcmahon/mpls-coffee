using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MPLSCoffee.Data;
using MPLSCoffee.Data.Models;

namespace MPLSCoffee.Harvester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            ConfigureServices(services, configuration);

            using var serviceProvider = services.BuildServiceProvider();
            await RunAsync(serviceProvider);
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConfiguration>(configuration);
            services.AddDbContext<CoffeeShopContext>(options =>
                options.UseMySql(configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))));
            services.AddSingleton<GooglePlacesService>(sp =>
                new GooglePlacesService(configuration["GoogleApi:ApiKey"]));
        }

        static async Task RunAsync(IServiceProvider serviceProvider)
        {
            var googleService = serviceProvider.GetRequiredService<GooglePlacesService>();
            var dbContext = serviceProvider.GetRequiredService<CoffeeShopContext>();

            try
            {
                Console.WriteLine("Fetching coffee shops in Minneapolis...");
                var coffeeShops = await googleService.GetCoffeeShopsInMinneapolis();
                Console.WriteLine($"Found {coffeeShops.Count} coffee shops in Minneapolis");

                foreach (var shopDetails in coffeeShops)
                {
                    var existingShop = await dbContext.CoffeeShops
                        .Include(cs => cs.Hours)
                        .FirstOrDefaultAsync(cs => cs.PlaceId == shopDetails.PlaceId);

                    if (existingShop == null)
                    {
                        existingShop = new CoffeeShop
                        {
                            PlaceId = shopDetails.PlaceId
                        };
                        dbContext.CoffeeShops.Add(existingShop);
                    }

                    // Update shop details
                    existingShop.Name = shopDetails.Name;
                    existingShop.Address = shopDetails.Address;
                    existingShop.Rating = shopDetails.Rating;
                    existingShop.UserRatingsTotal = shopDetails.UserRatingsTotal;
                    existingShop.Latitude = shopDetails.Latitude;
                    existingShop.Longitude = shopDetails.Longitude;
                    existingShop.LastUpdated = DateTime.UtcNow;
                    existingShop.WeekdayText = string.Join("|", shopDetails.WeekdayText);
                    existingShop.Website = shopDetails.Website;

                    // Update hours
                    existingShop.Hours.Clear();
                    if (shopDetails.Hours != null)
                    {
                        foreach (var hours in shopDetails.Hours)
                        {
                            TimeSpan ParseTime(string timeString)
                            {
                                if (timeString.Length == 4 && int.TryParse(timeString, out int time))
                                {
                                    int hours = time / 100;
                                    int minutes = time % 100;
                                    return new TimeSpan(hours, minutes, 0);
                                }
                                return TimeSpan.Zero;
                            }

                            existingShop.Hours.Add(new CoffeeShopHours
                            {
                                DayOfWeek = hours.DayOfWeek,
                                OpenTime = ParseTime(hours.OpenTime),
                                CloseTime = ParseTime(hours.CloseTime)
                            });
                        }
                    }

                    await dbContext.SaveChangesAsync();
                    Console.WriteLine($"Saved/Updated: {existingShop.Name}");
                }

                Console.WriteLine("Finished processing coffee shops.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

    }
}
