using API.Extensions;
using Microsoft.AspNetCore.Identity;

namespace API.Entities
{
    public class AppUser : IdentityUser<int> // IdentityUser is a class provided by ASP.NET Core Identity
    {
        public required string FullName { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
        public required string Country { get; set; }
        public required string City { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string? PrefferedLanguage { get; set; }
        public string? TimeZone { get; set; }
        public ICollection<AppUserRole> UserRoles { get; set; } = [];
        public List<UserChatHistory> ChatHistories { get; set; } = [];
        public List<Reminder> Reminders { get; set; } = [];
    }
}
