using API.Entities;

namespace API.Interfaces
{
    public interface IUserChatHistoryRepository
    {
        void AddUserChatHistory(List<UserChatHistory> chatHistory);
        Task<List<UserChatHistory>> GetUserChatHistory(int userId);
        Task<bool> SaveAllAsync();
    }
}
