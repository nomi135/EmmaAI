namespace API.DTOs
{
    public class UserChatHistoryDto
    {
        public int Id { get; set; }
        public string? ChatRole { get; set; }
        public string? Message { get; set; }
        public DateTime MessageSent { get; set; }
    }
}
