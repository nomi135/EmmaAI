using API.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text.Json;

namespace API.Services
{
    public class ChatHandler(
                       IWeatherService weatherService,
                       INewsService newsService,
                       IUserInfoService userInfoService,
                       Kernel kernel,
                       IMemoryCache cache) : IChatHandler
    {
        // Cache expiration period – adjust as needed.
        MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(1));

        [Description("Processes user input and returns a response based on the detected intent.")]
        public async Task<string?> ProcessUserInputAsync(string userInput)
        {
            // Detect intent dynamically.
            string? intent = await DetectIntentAsync(userInput);

            string? response = string.Empty;
            switch (intent)
            {
                case "GetCurrentDateTime":
                    string? timeZone = await userInfoService.GetUserInfoAsync("timezone");
                    response = await GetCurrentDateTimeAsync(userInput, timeZone);
                    break;
                case "GetWeatherUpdate":
                    string? location = await userInfoService.GetUserInfoAsync("location");
                    if (location != null)
                    {
                        string weatherCacheKey = $"weather_{location}_{DateTime.UtcNow:yyyyMMdd}";
                        if (!cache.TryGetValue(weatherCacheKey, out string? cachedWeather))
                        {
                            cachedWeather = await weatherService.GetCurrentWeatherAsync(location);
                            cache.Set(weatherCacheKey, cachedWeather, cacheEntryOptions);
                        }
                        response = cachedWeather;
                    }
                    break;

                case "GetLatestNews":
                    string? country = await userInfoService.GetUserInfoAsync("country");
                    if (country != null)
                    {
                        string newsCacheKey = $"news_{country}_{DateTime.UtcNow:yyyyMMdd}";
                        if (!cache.TryGetValue(newsCacheKey, out string? cachedNews))
                        {
                            cachedNews = await newsService.GetLatestNewsAsync(country);
                            cache.Set(newsCacheKey, cachedNews, cacheEntryOptions);
                        }
                        response = cachedNews;
                    }
                    break;

                case "GetUserInfo(location)":
                    response = await userInfoService.GetUserInfoAsync("location");
                    break;
                case "GetUserInfo(city)":
                    response = await userInfoService.GetUserInfoAsync("city");
                    break;
                case "GetUserInfo(country)":
                    response = await userInfoService.GetUserInfoAsync("country");
                    break;
                case "GetUserInfo(timezone)":
                    response = await userInfoService.GetUserInfoAsync("timezone");
                    break;
                default:
                    response = null;
                    break;
            }

            return response;
        }

        [Description("Gets the current date or time")]
        private static async Task<string> GetCurrentDateTimeAsync(string userInput, string? timeZone)
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

            string response = string.Empty;
            //detect whether user asked for "date" or "time"
            if (userInput.Contains("date", StringComparison.CurrentCultureIgnoreCase))
            {
                response = $"The date today is {dateTime.ToShortDateString()}";
            }
            else if (userInput.Contains("time", StringComparison.CurrentCultureIgnoreCase))
            {
                response = $"The Time now is {dateTime.ToString("hh:mm tt")}";
            }

            return await Task.FromResult(response);
        }

        private async Task<string?> DetectIntentAsync(string userInput)
        {
            var prompt = @$"
                            Instructions: What is the intent of this request?
                            If you don't know the intent, don't guess; instead respond with ""Unknown"".
                            Choices: GetCurrentDateTime, GetWeatherUpdate, GetLatestNews, GetUserInfo.

                            User Input: What is date today?
                            Intent: GetCurrentDateTime 

                            User Input: What is time now?
                            Intent: GetCurrentDateTime

                            User Input: How is weather today?
                            Intent: GetWeatherUpdate

                            User Input: Can you tell me latest updates?
                            Intent: GetLatestNews

                            User Input: What is my location?
                            Intent: GetUserInfo(location)

                            User Input: What is my timezone?
                            Intent: GetUserInfo(timezone)
                            
                            User Input: What is my country?
                            Intent: GetUserInfo(country)
                
                            User Input: What is my city?
                            Intent: GetUserInfo(city)

                            User Query: ""{userInput}""
                            Intent: ";

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            if (chatCompletionService != null)
            {
                var chatMessageContent = await chatCompletionService.GetChatMessageContentsAsync(prompt);

                // Use LINQ to get the first TextContent item
                var textItem = chatMessageContent
                    .SelectMany(a => a.Items)
                    .OfType<TextContent>()  // Ensure we're only selecting TextContent
                    .FirstOrDefault();

                return string.IsNullOrEmpty(textItem?.Text) ? "General" : textItem.Text;
            }
            return null;
        }

        [Description("Get Chat History")]
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
        private static async Task<ChatHistory> CreateChatHistorySystemMessage(string userName)
        {
            var history = new ChatHistory();
            history.AddSystemMessage("You are Emma AI, a helpful and intelligent virtual assistant. You can answer questions about the weather, news, and other topics. You can also help users with their tasks and provide information about their contacts.");
            history.AddSystemMessage($"userName is {userName}");
            return await Task.FromResult(history);
        }
    }
}
