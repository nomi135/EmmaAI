using API.Entities;
using API.Interfaces;
using System.ComponentModel;
using System.Security.Claims;

namespace API.Services
{
    public class UserInfoService(IHttpContextAccessor httpContextAccessor) : IUserInfoService //cannot use IHttpContextAccessor due to hangfire
    {
        [Description("Gets the user information")]
        public async Task<string?> GetUserInfoAsync(string property)
        {
            // Get the user entity based on the username
            var user = await GetUserAsync();
            if (user == null)
                return null;

            // Based on the info parameter, return the corresponding property value
            return property.ToLower() switch
            {
                "timezone" => user.TimeZone,
                "city" => user.City,
                "country" => user.Country,
                "language" => user.PrefferedLanguage,
                "email" => user.Email,
                "fullname" => user.FullName,
                "username" => user.UserName,
                "location" => user.Latitude?.ToString() + "," + user.Longitude?.ToString(),
                _ => null // If the info parameter doesn't match any known property, return null
            };
        }

        [Description("Gets the user entity")]
        public async Task<AppUser?> GetUserAsync()
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return null;

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            var repo = httpContext.RequestServices.GetRequiredService<IUserRepository>();
            var userId = int.Parse(userIdClaim);
            var user = await repo.GetUserByIdAsync(userId);
            return user;
        }

        [Description("Gets the current date or time")]
        public async Task<string?> GetCurrentDateTimeAsync(string? timeZone)
        {

            if (timeZone == null)
            {
                return "User timezone not found";
            }

            var offsetParts = timeZone.Split(new[] { '+', '-' });
            if (offsetParts.Length != 2) return "Invalid timezone format";

            var sign = timeZone.Contains('+') ? 1 : -1;
            var hours = int.Parse(offsetParts[1].Split(':')[0]);
            var minutes = int.Parse(offsetParts[1].Split(':')[1]);

            DateTime dateTime = DateTime.UtcNow.AddHours(hours * sign).AddMinutes(minutes * sign);

            return await Task.FromResult(dateTime.ToString("dd/MM/yyyy hh:mm tt"));
        }
    }
}
