namespace API.Entities
{
    public class UserChatHistory
    {
        public int Id { get; set; }
        public required string ChatRole { get; set; }
        public required string Message { get; set; }
        public DateTime MessageSent { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
    }
}
