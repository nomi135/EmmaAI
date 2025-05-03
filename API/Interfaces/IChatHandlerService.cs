using API.DTOs;
using Microsoft.SemanticKernel.ChatCompletion;

namespace API.Interfaces
{
    public interface IChatHandlerService
    {
        Task<(string? result, IntentDto intent)> ProcessUserInputAsync(string userInput);
        Task<ChatHistory?> GetChatHistoryAsync(string userName);
        Task<string> SaveChatHistoryAsync(string userName, ChatHistory history);
    }
}
