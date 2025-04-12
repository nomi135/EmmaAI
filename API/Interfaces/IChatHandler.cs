using Microsoft.SemanticKernel.ChatCompletion;

namespace API.Interfaces
{
    public interface IChatHandler
    {
        Task<string?> ProcessUserInputAsync(string userInput);
        Task<ChatHistory?> GetChatHistoryAsync(string username);
        Task<string> SaveChatHistoryAsync(string username, ChatHistory history);
    }
}
