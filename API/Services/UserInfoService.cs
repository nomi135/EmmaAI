using API.Interfaces;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Security.Claims;

namespace API.Services
{
    public class UserInfoService(IHttpContextAccessor httpContextAccessor) : IUserInfoService
    {
        [Description("Gets the user information")]
        public async Task<string?> GetUserInfoAsync(string property)
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
    }
}
