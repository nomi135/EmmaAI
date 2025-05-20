using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace API.Data
{
    public class ReminderRepository(DataContext context, IMapper mapper) : IReminderRepository
    {
        public void AddReminder(Reminder reminder)
        {
            context.reminders.Add(reminder);
        }

        public async Task<List<ReminderDto>> GetUserRemindersAsync(string userName, string dateNow)
        {
            DateTime dateTime = DateTime.ParseExact(dateNow, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);

            return await context.reminders
                .Where(r => r.AppUser.UserName == userName && !r.IsTriggered && r.ReminderTime > dateTime)
                .OrderBy(x => x.ReminderTime)
                .ProjectTo<ReminderDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<Reminder?> GetReminderAsync(int Id)
        {
            return await context.reminders
                .Where(r => r.Id == Id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ReminderDto>> GetUserDueRemindersAsync(string userName, string dateNow)
        {
            DateTime dateTime = DateTime.ParseExact(dateNow, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);

            var reminders = await context.reminders
                .Where(r => r.AppUser.UserName == userName && !r.IsTriggered && r.ReminderTime == dateTime)
                .OrderBy(x => x.ReminderTime)
                .ProjectTo<ReminderDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return reminders;
        }

        public void UpdateReminder(Reminder reminder)
        {
            context.Entry(reminder).State = EntityState.Modified;
        }

    }
}
