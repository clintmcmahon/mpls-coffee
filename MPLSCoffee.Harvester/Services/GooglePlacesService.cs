using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Linq;
using MPLSCoffee.Data;

namespace MPLSCoffee.Harvester
{
    public class GooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string PlacesApiBaseUrl = "https://maps.googleapis.com/maps/api/place";
        private const string GeocodingApiBaseUrl = "https://maps.googleapis.com/maps/api/geocode";

        // Minneapolis bounding box
        private const double MinLat = 44.8894;
        private const double MaxLat = 45.0512;
        private const double MinLng = -93.3299;
        private const double MaxLng = -93.1951;

        // List of Minneapolis zip codes
        /*
        private readonly string[] MinneapolisZipCodes = new string[]
        {
            "55401", "55402", "55403", "55404", "55405", "55406", "55407", "55408", "55409", "55410",
            "55411", "55412", "55413", "55414", "55415", "55416", "55417", "55418", "55419", "55420",
            "55421", "55422", "55423", "55424", "55425", "55426", "55427", "55428", "55429", "55430",
            "55431", "55432", "55433", "55434", "55435", "55436", "55437", "55438", "55439", "55440",
            "55441", "55442", "55443", "55444", "55445", "55446", "55447", "55448", "55449", "55450",
            "55454", "55455"
        };*/

        private readonly string[] MinneapolisZipCodes = new string[]
       {
            "55401"
       };

        public GooglePlacesService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<List<CoffeeShopDetails>> GetCoffeeShopsInMinneapolis()
        {
            var allResults = new HashSet<PlaceResult>(new PlaceResultComparer());

            foreach (var zipCode in MinneapolisZipCodes)
            {
                var coordinates = await GetZipCodeCoordinates(zipCode);
                if (coordinates != null)
                {
                    var zipResults = await SearchCoffeeShopsByCoordinates(coordinates.Value.lat, coordinates.Value.lng);
                    foreach (var result in zipResults)
                    {
                        if (IsWithinMinneapolis(result.Geometry.Location))
                        {
                            allResults.Add(result);
                        }
                    }
                    Console.WriteLine($"Processed zip code {zipCode}. Total unique results so far: {allResults.Count}");
                }
            }

            var detailedResults = new List<CoffeeShopDetails>();
            foreach (var result in allResults)
            {
                var details = await GetPlaceDetails(result.PlaceId);
                detailedResults.Add(details);
            }

            return detailedResults;
        }

        private async Task<(double lat, double lng)?> GetZipCodeCoordinates(string zipCode)
        {
            var url = $"{GeocodingApiBaseUrl}/json?address={zipCode}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var geocodingResponse = JsonSerializer.Deserialize<GeocodingResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (geocodingResponse.Results.Any())
            {
                var location = geocodingResponse.Results[0].Geometry.Location;
                return (location.Lat, location.Lng);
            }

            return null;
        }

        private async Task<List<PlaceResult>> SearchCoffeeShopsByCoordinates(double lat, double lng)
        {
            var results = new List<PlaceResult>();
            string nextPageToken = null;

            do
            {
                var url = $"{PlacesApiBaseUrl}/nearbysearch/json?location={lat},{lng}&radius=2000&type=cafe&keyword=coffee&key={_apiKey}";
                if (!string.IsNullOrEmpty(nextPageToken))
                {
                    url += $"&pagetoken={nextPageToken}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var searchResponse = JsonSerializer.Deserialize<NearbySearchResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (searchResponse.Results != null)
                {
                    results.AddRange(searchResponse.Results);
                }

                nextPageToken = searchResponse.NextPageToken;

                if (!string.IsNullOrEmpty(nextPageToken))
                {
                    await Task.Delay(2000); // Wait 2 seconds before the next request
                }
            }
            while (!string.IsNullOrEmpty(nextPageToken));

            return results;
        }

        private bool IsWithinMinneapolis(Location location)
        {
            return location.Lat >= MinLat && location.Lat <= MaxLat &&
                   location.Lng >= MinLng && location.Lng <= MaxLng;
        }

        private async Task<CoffeeShopDetails> GetPlaceDetails(string placeId)
        {
            var url = $"{PlacesApiBaseUrl}/details/json?place_id={placeId}&fields=name,website,formatted_address,opening_hours,rating,user_ratings_total,geometry&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var detailsResponse = JsonSerializer.Deserialize<PlaceDetailsResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (detailsResponse?.Result == null)
            {
                throw new Exception($"Failed to get details for place ID: {placeId}");
            }

            var result = detailsResponse.Result;

            return new CoffeeShopDetails
            {
                PlaceId = placeId,
                Name = result.Name ?? "Unknown",
                Address = result.FormattedAddress ?? "Address not available",
                Rating = result.Rating ?? 0,
                UserRatingsTotal = result.UserRatingsTotal ?? 0,
                Latitude = result.Geometry?.Location?.Lat ?? 0,
                Longitude = result.Geometry?.Location?.Lng ?? 0,
                Website = result.Website ?? "",
                Hours = result.OpeningHours?.Periods?.Select(p => new OpeningHours
                {
                    DayOfWeek = p.Open?.Day ?? 0,
                    OpenTime = p.Open?.Time ?? "0000",
                    CloseTime = p.Close?.Time ?? "0000"
                }).ToList() ?? new List<OpeningHours>(),
                WeekdayText = result.OpeningHours?.WeekdayText ?? new List<string>()
            };
        }
    }

    public class PlaceResultComparer : IEqualityComparer<PlaceResult>
    {
        public bool Equals(PlaceResult x, PlaceResult y)
        {
            return x.PlaceId == y.PlaceId;
        }

        public int GetHashCode(PlaceResult obj)
        {
            return obj.PlaceId.GetHashCode();
        }
    }

    public class NearbySearchResponse
    {
        public List<PlaceResult> Results { get; set; }

        [JsonPropertyName("next_page_token")]
        public string NextPageToken { get; set; }

        public string Status { get; set; }
    }

    public class GeocodingResponse
    {
        public List<GeocodingResult> Results { get; set; }
        public string Status { get; set; }
    }

    public class GeocodingResult
    {
        public Geometry Geometry { get; set; }
    }

    public class PlaceResult
    {
        [JsonPropertyName("place_id")]
        public string PlaceId { get; set; }
        public string Name { get; set; }
        public string Vicinity { get; set; }
        public Geometry Geometry { get; set; }
        public double? Rating { get; set; }

        [JsonPropertyName("user_ratings_total")]
        public int? UserRatingsTotal { get; set; }
    }

    public class Geometry
    {
        public Location Location { get; set; }
    }

    public class Location
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class PlaceDetailsResponse
    {
        public PlaceDetailsResult Result { get; set; }
        public string Status { get; set; }
    }

    public class PlaceDetailsResult
    {
        public string Name { get; set; }

        [JsonPropertyName("formatted_address")]
        public string FormattedAddress { get; set; }

        [JsonPropertyName("opening_hours")]
        public OpeningHoursDetail OpeningHours { get; set; }

        public double? Rating { get; set; }

        [JsonPropertyName("user_ratings_total")]
        public int? UserRatingsTotal { get; set; }

        public Geometry Geometry { get; set; }
        public string Website { get; set; }
    }

    public class OpeningHoursDetail
    {
        [JsonPropertyName("open_now")]
        public bool OpenNow { get; set; }

        public List<Period> Periods { get; set; }

        [JsonPropertyName("weekday_text")]
        public List<string> WeekdayText { get; set; }
    }

    public class Period
    {
        public DayTime Open { get; set; }
        public DayTime Close { get; set; }
    }

    public class DayTime
    {
        public int Day { get; set; }
        public string Time { get; set; }
    }

    public class CoffeeShopDetails
    {
        public string PlaceId { get; set; } = "";
        public string Name { get; set; } = "Unknown";
        public string Address { get; set; } = "Address not available";
        public double Rating { get; set; } = 0;
        public int UserRatingsTotal { get; set; } = 0;
        public double Latitude { get; set; } = 0;
        public double Longitude { get; set; } = 0;
        public string Website { get; set; } = "";
        public List<OpeningHours> Hours { get; set; } = new List<OpeningHours>();
        public List<string> WeekdayText { get; set; } = new List<string>();
    }

    public class OpeningHours
    {
        public int DayOfWeek { get; set; }
        public string OpenTime { get; set; } = "0000";
        public string CloseTime { get; set; } = "0000";
    }

}
