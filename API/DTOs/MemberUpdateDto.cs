using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class MemberUpdateDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? PrefferedLanguage { get; set; }
        public string? TimeZone { get; set; }
    }
}
