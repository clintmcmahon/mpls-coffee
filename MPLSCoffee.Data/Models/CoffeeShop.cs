using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MPLSCoffee.Data.Models
{
    public class CoffeeShop
    {
        [Key]
        public string PlaceId { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Rating { get; set; }
        public int? UserRatingsTotal { get; set; }
        public string? WeekdayText { get; set; }
        public string? Website { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool? IsGood { get; set; }
        public List<CoffeeShopHours>? Hours { get; set; } = new List<CoffeeShopHours>();
    }

    public class CoffeeShopHours
    {
        public int Id { get; set; }
        public string CoffeeShopPlaceId { get; set; }
        public CoffeeShop CoffeeShop { get; set; }
        public int DayOfWeek { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
    }
}
