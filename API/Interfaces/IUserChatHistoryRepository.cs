using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IUserChatHistoryRepository
    {
        void AddUserChatHistory(List<UserChatHistory> chatHistory);
        Task<List<UserChatHistoryDto>> GetUserChatHistoryAsync(int userId);
    }
}
