namespace API.Entities
{
    public class Reminder
    {
        public int Id { get; set; }
        public string Task { get; set; } = string.Empty;
        public DateTime ReminderTime { get; set; }
        public bool IsTriggered { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        //Navigation properties
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
    }
}
