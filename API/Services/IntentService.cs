﻿using API.DTOs;
using API.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace API.Services
{
    public class IntentService(Kernel kernel, IWeatherService weatherService, INewsService newsService, IUserInfoService userInfoService,
                               IDocumentService documentService, IReminderService reminderService, IMemoryCache cache) : IIntentService
    {
        // Cache expiration period – adjust as needed.
        MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(1));

        [Description("Detects the intent of the user input using a prompt template.")]
        public async Task<IntentDto?> DetectIntentAsync(string userInput)
        {
            IntentDto intent = new IntentDto();
            var promptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", "IntentDetectionPrompt.txt");
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

        [Description("Gets a response based on the detected intent.")]
        public async Task<string?> GetIntentBasedResponseAsync(IntentDto? intent, string userQuery, string username)
        {
            string? response = string.Empty;
            switch (intent.Intent)
            {
                case "GetCurrentDateTime":
                    string? timeZone = intent.TimeZone;
                    if (string.IsNullOrEmpty(timeZone))
                    {
                        timeZone = await userInfoService.GetUserInfoAsync("timezone");
                    }
                    response = await userInfoService.GetCurrentDateTimeAsync(timeZone);
                    break;

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
                                if (!string.IsNullOrWhiteSpace(intent.City))
                                {
                                    cachedWeather = $"The temperature in {intent.City} is: {weatherData}";
                                }
                                else
                                {
                                    cachedWeather = $"The temperature is: {weatherData}";
                                }
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
                            if (!string.IsNullOrEmpty(headlines) && headlines != "Failed to fetch news data")
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
                case "SearchDocument":
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        string documentSearchResult = await documentService.SearchDocumentAsync(username, userQuery);
                        response = documentSearchResult;
                    }
                    break;
                case "SetReminder":
                    ReminderDto reminder = new ReminderDto()
                    {
                        UserName = username,
                        Task = !string.IsNullOrWhiteSpace(intent.ReminderTask) ? intent.ReminderTask : ""
                    };
                    timeZone = await userInfoService.GetUserInfoAsync("timezone");
                    if (!string.IsNullOrWhiteSpace(timeZone))
                    {
                       string currentDateTime = await userInfoService.GetCurrentDateTimeAsync(timeZone);
                        //extract date and time in user timezone format relative to current date time from user prompt as intital intent detection function can only detect intent
                        userQuery = $"DateTime right now in {timeZone} is {currentDateTime}. Extract only Date and Time(combined) in resoponse from following user query relative to mentioned " +
                            $"Return DateTime in format dd/MM/yyyy hh:mm tt: {userQuery}";
                        var chatMessageContent = await kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentsAsync(userQuery);
                        // Use LINQ to get the first TextContent item
                        var textItem = chatMessageContent
                            .SelectMany(a => a.Items)
                            .OfType<TextContent>()  // Ensure we're only selecting TextContent
                            .FirstOrDefault();
                        if (textItem != null && !string.IsNullOrEmpty(textItem.Text) && textItem.Text != currentDateTime)
                        {
                            reminder.ReminderTime = DateTime.ParseExact(textItem.Text, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None);
                        }
                    }
                    if (reminder.ReminderTime == DateTime.MinValue)
                    {
                        response = "Please provide a valid date and time for the reminder.";
                        break;
                    }
                    else if (!string.IsNullOrWhiteSpace(username))
                    {
                        var user = await userInfoService.GetUserAsync();
                        reminder.AppUserId = user.Id;
                        await reminderService.SetReminderAsync(reminder);
                        response = $"Reminder for Task {reminder.Task} has been set at {reminder.ReminderTime}";
                    }
                    break;
                default:
                    response = null;
                    break;
            }
            return await Task.FromResult(response);
        }
    }
}
