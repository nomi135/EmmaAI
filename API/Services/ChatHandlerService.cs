using API.DTOs;
using API.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text.Json;

namespace API.Services
{
    public class ChatHandlerService(IIntentService intentService, IUserInfoService userInfoService, IMemoryCache cache) : IChatHandlerService
    {
        // Cache expiration period – adjust as needed.
        MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(1));

        [Description("Processes user input and returns a response based on the detected intent.")]
        public async Task<(string? result, IntentDto intent)> ProcessUserInputAsync(string userInput)
        {
            string? response = null;
            // Detect intent dynamically.
            IntentDto? intent = await intentService.DetectIntentAsync(userInput);
            if (!string.IsNullOrWhiteSpace(intent.Intent))
            {
                response = await intentService.GetIntentBasedResponseAsync(intent);
            }
            return (response, intent);
        }

        [Description("Gets the current date or time")]
        private static async Task<string> GetCurrentDateTimeAsync(string? timeZone)
        {

            if (timeZone == null)
            {
                return "User timezone not found";
            }

            var offsetParts = timeZone.Split(new[] { '+', '-' });
            if (offsetParts.Length != 2) return "Invalid timezone format";

            var sign = timeZone.Contains('+') ? 1 : -1;
            var hours = int.Parse(offsetParts[1].Split(':')[0]);
            var minutes = int.Parse(offsetParts[1].Split(':')[1]);

            DateTime dateTime = DateTime.UtcNow.AddHours(hours * sign).AddMinutes(minutes * sign);

            return await Task.FromResult(dateTime.ToString());
        }

        [Description("Gets the chat history for a user. If not found in cache, creates a new one.")]
        public async Task<ChatHistory?> GetChatHistoryAsync(string userName)
        {
            var chatHistory = new ChatHistory();
            string chatHistoryCacheKey = $"chatHistory_{userName}";
            if (!cache.TryGetValue(chatHistoryCacheKey, out string? cachedChatHistory))
            {
                chatHistory = await CreateChatHistorySystemMessage(userName);
                // Serialize the chat history to a string
                cachedChatHistory = JsonSerializer.Serialize(chatHistory);
                cache.Set(chatHistoryCacheKey, cachedChatHistory, cacheEntryOptions);
            }
            else if (cachedChatHistory != null)
            {
                // Deserialize the cached chat history
                chatHistory = JsonSerializer.Deserialize<ChatHistory>(cachedChatHistory);
            }

            return chatHistory;
        }

        [Description("Save Chat History")]
        public async Task<string> SaveChatHistoryAsync(string userName, ChatHistory history)
        {
            string chatHistoryCacheKey = $"chatHistory_{userName}";
            string serilized = JsonSerializer.Serialize(history);
            var result = cache.Set(chatHistoryCacheKey, serilized, cacheEntryOptions);
            return await Task.FromResult(result);
        }

        [Description("Creates a system message for the chat history.")]
        private async Task<ChatHistory> CreateChatHistorySystemMessage(string userName)
        {
            var user = await userInfoService.GetUserAsync();
            string location = await userInfoService.GetUserInfoAsync("location");
            var history = new ChatHistory();

            // Load prompt from external file
            /*var promptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", "IntentDetectionPrompt.txt");

            if (File.Exists(promptFilePath))
            {
                var systemPrompt = await File.ReadAllTextAsync(promptFilePath);
                history.AddSystemMessage(systemPrompt);
            }
            */
            // Inject user information
            history.AddSystemMessage(
                $"Here is complete user information:\n" +
                $"- UserName: {userName}\n" +
                $"- Email: {user.Email}\n" +
                $"- Full Name: {user.FullName}\n" +
                $"- Country: {user.Country}\n" +
                $"- City: {user.City}\n" +
                $"- Location: {location}\n" +
                $"- TimeZone: {user.TimeZone}");

            // Date and time
            history.AddSystemMessage($"Current date and time: {await GetCurrentDateTimeAsync(user.TimeZone)}");

            return await Task.FromResult(history);
        }

    }

}