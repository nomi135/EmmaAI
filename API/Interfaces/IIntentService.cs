using API.DTOs;

namespace API.Interfaces
{
    public interface IIntentService
    {
        Task<IntentDto?> DetectIntentAsync(string userInput);
        Task<string?> GetIntentBasedResponseAsync(IntentDto? intent, string userQuery, string userName);
    }
}
