using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class ReminderController(IReminderService reminderService) : BaseApiController
    {
        [HttpGet("{username}")]
        public async Task<ActionResult<List<ReminderDto>>> GetActiveReminders(string username)
        {
            var reminders = await reminderService.GetUserRemindersAsync(username);

            if (reminders == null) return NotFound();

            return reminders;
        }

    }
}
