using API.Entities;

namespace API.Interfaces
{
    public interface IUserInfoService
    {
        Task<string?> GetUserInfoAsync(string property);

        Task<AppUser?> GetUserAsync();

        Task<string?> GetCurrentDateTimeAsync(string? timeZone);
    }
}
