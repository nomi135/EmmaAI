namespace API.Interfaces
{
    public interface IUserInfoService
    {
        Task<string?> GetUserInfoAsync(string property);
    }
}
