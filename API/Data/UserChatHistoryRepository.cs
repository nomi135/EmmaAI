using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserChatHistoryRepository(DataContext context) : IUserChatHistoryRepository
    {
        public void AddUserChatHistory(List<UserChatHistory> chatHistory)
        {
            context.AddRangeAsync(chatHistory);
        }

        public async Task<List<UserChatHistory>> GetUserChatHistory(int userId)
        {
            return await context.ChatHistories
                .Where(x => x.AppUserId == userId)
                .OrderByDescending(x => x.MessageSent)
                .ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }
    }
}
