using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IReminderService
    {
        Task SetReminderAsync(ReminderDto reminderDtos);
        Task<List<ReminderDto>> GetUserRemindersAsync(string userName);
        Task<Reminder?> GetReminderAsync(int Id);
        Task<List<ReminderDto>> GetUserDueRemindersAsync(string userName);
        Task<bool> UpdateReminderAsync(Reminder reminderDto);
    }
}
