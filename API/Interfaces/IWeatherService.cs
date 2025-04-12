namespace API.Interfaces
{
    public interface IWeatherService
    {
        Task<string?> GetCurrentWeatherAsync(string location);
    }
}
