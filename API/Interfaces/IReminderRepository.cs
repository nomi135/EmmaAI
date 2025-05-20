using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IReminderRepository
    {
        void AddReminder(Reminder reminder);
        Task<List<ReminderDto>> GetUserRemindersAsync(string userName, string dateNow);
        Task<Reminder?> GetReminderAsync(int Id);
        Task<List<ReminderDto>> GetUserDueRemindersAsync(string userName, string dateNow);
        void UpdateReminder(Reminder reminder);
    }
}
