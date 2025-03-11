using System.ComponentModel.DataAnnotations;

namespace API.Entities
{
    public class Contact
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        [EmailAddress]
        public required string Email { get; set; }
        public required string Subject { get; set; }
        public required string Message { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}