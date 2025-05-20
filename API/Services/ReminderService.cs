using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace API.Services
{
    public class ReminderService(IUnitOfWork unitOfWork, IUserInfoService userInfoService, IMapper mapper) : IReminderService
    {
        [Description("Set a reminder for the user.")]
        public async Task SetReminderAsync(ReminderDto reminderDto)
        {
            // Save reminder to DB (so it shows up in UI later)
            var reminder = mapper.Map<Reminder>(reminderDto);
            unitOfWork.ReminderRepository.AddReminder(reminder);
            bool result = await unitOfWork.Complete();
        }

        [Description("Get all active reminders for the user")]
        public async Task<List<ReminderDto>> GetUserRemindersAsync(string userName)
        {
            string dateNow = await userInfoService.GetCurrentDateTimeAsync(await userInfoService.GetUserInfoAsync("timezone"));
            if(string.IsNullOrWhiteSpace(dateNow))
            {
                dateNow = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");
            }
            return await unitOfWork.ReminderRepository.GetUserRemindersAsync(userName, dateNow);  
        }

        [Description("Get reminder by Id")]
        public async Task<Reminder?> GetReminderAsync(int Id)
        {
            return await unitOfWork.ReminderRepository.GetReminderAsync(Id);
        }

        [Description("Get user reminder for the notification")]
        public async Task<List<ReminderDto>> GetUserDueRemindersAsync(string userName)
        {
            //have to reaccess user through repository because GetUserDueRemindersAsync function is called from hangfire background job and httpcontext is null in that case
            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(userName);
            string timeZone = user.TimeZone;
            string dateNow = await userInfoService.GetCurrentDateTimeAsync(timeZone);
            if (string.IsNullOrWhiteSpace(dateNow))
            {
                dateNow = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");
            }
            return await unitOfWork.ReminderRepository.GetUserDueRemindersAsync(userName, dateNow);
        }

        [Description("Update reminder status to istriggered")]
        public async Task<bool> UpdateReminderAsync(Reminder reminder)
        {
            unitOfWork.ReminderRepository.UpdateReminder(reminder);
            return await unitOfWork.Complete();
        }

    }
}
