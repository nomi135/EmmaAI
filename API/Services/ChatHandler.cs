using API.DTOs;
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
            IntentDto? intent = await DetectIntentAsync(userInput);

            string? response = string.Empty;
            switch (intent.Intent)
            {
                case "GetWeatherUpdate":
                    string? location = intent.Location;
                    if (string.IsNullOrEmpty(location) && string.IsNullOrEmpty(intent.City))
                    {
                        location = await userInfoService.GetUserInfoAsync("location");
                    }
                    if (location != null)
                    {
                        string weatherCacheKey = $"weather_{location}_{DateTime.UtcNow:yyyyMMdd}";
                        if (!cache.TryGetValue(weatherCacheKey, out string? cachedWeather))
                        {
                            var weatherData = await weatherService.GetCurrentWeatherAsync(location);
                            if (weatherData != "User location not found" || weatherData != "Invalid location format" || weatherData != "Failed to fetch weather data")
                            {
                                cachedWeather = $"The temperature in {intent.City} is: {weatherData}";
                                cache.Set(weatherCacheKey, cachedWeather, cacheEntryOptions);
                            }
                            else
                            {
                                cachedWeather = weatherData;
                            }
                        }
                        response = cachedWeather;
                    }
                    break;
                case "GetLatestNews":
                    string? country = intent.CountryCode;
                    if (string.IsNullOrEmpty(country))
                    {
                        country = await userInfoService.GetUserInfoAsync("country");
                    }
                    if (country != null)
                    {
                        string newsCacheKey = $"news_{country}_{DateTime.UtcNow:yyyyMMdd}";
                        if (!cache.TryGetValue(newsCacheKey, out string? cachedNews))
                        {
                            var headlines = await newsService.GetLatestNewsAsync(country);
                            if (headlines != "Failed to fetch news data")
                            {
                                cachedNews = !string.IsNullOrEmpty(intent.Country) switch
                                {
                                   true => $"Here are the top 10 news headlines from {intent.Country} : {headlines}",
                                    _ => $"Here are the top 10 news headlines : {headlines}"
                                };
                            }
                            else
                            {
                                cachedNews = headlines;
                            }
                        }
                        response = cachedNews;
                    }
                    break;
                default:
                    response = null;
                    break;
            }

            return response;
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
            history.AddSystemMessage("You are Emma, a compassionate and emotionally intelligent AI partner. Respond with empathy, care, and understanding based on what the user is feeling." +
                                     "Your tone should feel like a kind, supportive spouse or friend. If the user is sad, be comforting. If they are angry, stay calm and supportive. " +
                                     "If they are anxious, be soothing. Celebrate with them if they’re happy.Keep replies warm, natural, and emotionally aware. Focus on connection, not just information. " +
                                     "Avoid robotic language.You can answer questions about the weather, news, and other topics. You can also help users with their tasks and provide information about their contacts.");
            history.AddSystemMessage($"Here is complete user information: UserName {userName}, Email: {user.Email}, FullName: {user.FullName}, " +
                                     $"Country: {user.Country}, City: {user.City}, Location: {location}, TimeZone: {user.TimeZone}");
            history.AddSystemMessage($"here is current date and time: {await GetCurrentDateTimeAsync(user.TimeZone)}");
            history.AddSystemMessage($"here is weather update for {user.City}: {await weatherService.GetCurrentWeatherAsync(location == null ? "" : location)}");
            history.AddSystemMessage($"here is top 10 news from {user.Country}: {await newsService.GetLatestNewsAsync(user.Country)}");
            return await Task.FromResult(history);
        }

        [Description("Detects the intent of the user input using a prompt template.")]
        private async Task<IntentDto?> DetectIntentAsync(string userInput)
        {
            IntentDto intent = new IntentDto();
            var promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Prompts", "IntentDetectionPrompt.txt");
            var promptTemplate = await File.ReadAllTextAsync(promptFilePath);

            // Replace {userInput} placeholder dynamically
            var prompt = promptTemplate.Replace("{userInput}", userInput);

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            if (chatCompletionService != null)
            {
                var chatMessageContent = await chatCompletionService.GetChatMessageContentsAsync(prompt);

                // Use LINQ to get the first TextContent item
                var textItem = chatMessageContent
                    .SelectMany(a => a.Items)
                    .OfType<TextContent>()  // Ensure we're only selecting TextContent
                    .FirstOrDefault();
                if (textItem != null && !string.IsNullOrEmpty(textItem.Text))
                {
                    // Deserialize the JSON response into the IntentDto object
                    intent = JsonSerializer.Deserialize<IntentDto>(textItem.Text, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            return intent;
        }
    }
}