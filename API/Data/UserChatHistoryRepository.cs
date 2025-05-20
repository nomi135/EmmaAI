using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserChatHistoryRepository(DataContext context, IMapper mapper) : IUserChatHistoryRepository
    {
        public void AddUserChatHistory(List<UserChatHistory> chatHistory)
        {
            context.AddRangeAsync(chatHistory);
        }

        public async Task<List<UserChatHistoryDto>> GetUserChatHistoryAsync(int userId)
        {
            return await context.ChatHistories
                .Where(x => x.AppUserId == userId)
                .OrderByDescending(x => x.MessageSent)
                .ProjectTo<UserChatHistoryDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }
    }
}
