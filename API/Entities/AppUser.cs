using API.Extensions;
using Microsoft.AspNetCore.Identity;

namespace API.Entities
{
    public class AppUser : IdentityUser<int> // IdentityUser is a class provided by ASP.NET Core Identity
    {
        public required string FullName { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
        public string? PrefferedLanguage { get; set; }
        public string? TimeZone { get; set; }
        public ICollection<AppUserRole> UserRoles { get; set; } = [];
    }
}
