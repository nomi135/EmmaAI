using API.Entities;
using API.Interfaces;
using API.SignalR;
using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.SignalR;

namespace API.HangFire
{
    public class ReminderJobService(IReminderService reminderService, ISpeechService speechService, PresenceTracker tracker, 
                                    IHubContext<PresenceHub> presenceHub, IMapper mapper)
    {
        [DisableConcurrentExecution(60)] // Lock for 60 seconds
        public async Task CheckAndPushReminders()
        {
            var activeUsers = tracker.GetOnlineUsers().Result;

            foreach (var userName in activeUsers)
            {
                var dueReminders = await reminderService.GetUserDueRemindersAsync(userName);

                if (dueReminders.Any())
                {
                    var connection = await PresenceTracker.GetConnectionsForUser(userName);
                    if (connection != null && connection?.Count != null)
                    {
                        foreach (var reminderDto in dueReminders)
                        {
                            reminderDto.AudioPath = await speechService.TextToSpeechAsync(userName, $"There is a reminder for you: {reminderDto.Task}");
                            await presenceHub.Clients.Clients(connection)
                                 .SendAsync("ReceiveReminder", new { reminderDto.Id, reminderDto.Task, reminderDto.AudioPath });

                            // Update the reminder status to triggered
                            reminderDto.IsTriggered = true;

                            var reminder = mapper.Map<Reminder>(reminderDto);
                            await reminderService.UpdateReminderAsync(reminder);
                        }
                    }
                }
            }
        }

    }
}
