using API.Interfaces;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace API.Services
{
    public class WeatherService(HttpClient httpClient) : IWeatherService
    {
        [Description("Gets the current date or time")]
        public async Task<string?> GetCurrentWeatherAsync(string location)
        {
            if (location == null)
            {
                return "User location not found";
            }
            string[] locationParts = location.Split(',');
            if (locationParts.Length != 2)
            {
                return "Invalid location format";
            }

            var url = "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current_weather=true";

            var response = await httpClient.GetAsync(string.Format(url, locationParts[0], locationParts[1]));
            if (!response.IsSuccessStatusCode)
            {
                return "Failed to fetch weather data";
            }
            var content = await response.Content.ReadAsStringAsync();
            var weatherData = System.Text.Json.JsonDocument.Parse(content);
            var currentWeather = weatherData.RootElement.GetProperty("current_weather");
            var temperature = currentWeather.GetProperty("temperature").GetDouble();
            return $"{temperature}°C";
        }
    }
}
