namespace API.Interfaces
{
    public interface INewsService
    {
        Task<string?> GetLatestNewsAsync(string county);
    }
}
