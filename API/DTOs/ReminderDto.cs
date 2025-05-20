namespace API.DTOs
{
    public class ReminderDto
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Task { get; set; } = string.Empty;
        public string? AudioPath { get; set; }
        public DateTime ReminderTime { get; set; }
        public bool IsTriggered { get; set; } = false;
    }
}
