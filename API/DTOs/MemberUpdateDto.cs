using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class MemberUpdateDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string? PrefferedLanguage { get; set; }
        public string? TimeZone { get; set; }
    }
}
