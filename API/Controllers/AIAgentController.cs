using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using API.Middleware;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace API.Controllers
{
    [Authorize]
    public class AIAgentController(IUserChatHistoryRepository chatHistoryRepository, IChatHandlerService chatHandlerService, ISpeechService speechService, Kernel kernel,
                                   AzureOpenAIPromptExecutionSettings executionSettings, IMapper mapper) : BaseApiController
    {
        [HttpPost]
        public async Task<ActionResult<AssistantMessageDto>> Chat([FromBody] UserMessageDto userMessage)
        {
            ChatHistory history = new ChatHistory();

            var chatHistory = await chatHandlerService.GetChatHistoryAsync(User.GetUsername());
            if (chatHistory != null)
            {
                history = chatHistory;
            }

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            if (chatService == null)
            {
                return BadRequest("Chat service not found.");
            }

            history.AddUserMessage(userMessage.Message);

            var (result, intent) = await chatHandlerService.ProcessUserInputAsync(userMessage.Message);
            var textItem = new TextContent(result);
            if (string.IsNullOrEmpty(result))
            {
                if (!string.IsNullOrWhiteSpace(intent.ResponseStyle))
                {
                    history.AddSystemMessage($"Detected emotion:{intent.Emotion} reply in: {intent.ResponseStyle} tone.");
                }
                var chatMessageContent = await chatService.GetChatMessageContentsAsync(chatHistory: history, kernel: kernel, executionSettings: executionSettings);

                if (chatMessageContent == null)
                    return BadRequest("No response found from agent.");

                // Use LINQ to get the first TextContent item
                textItem = chatMessageContent
                    .SelectMany(a => a.Items)
                    .OfType<TextContent>()  // Ensure we're only selecting TextContent
                    .FirstOrDefault();

                if (textItem == null)
                {
                    return BadRequest("No valid text response found.");
                }
            }
            // Map the single item to DTO
            var assistantMessage = mapper.Map<AssistantMessageDto>(textItem);

            history.AddAssistantMessage(assistantMessage.Text);

            // Update chat history cache
            string cache = await chatHandlerService.SaveChatHistoryAsync(User.GetUsername(), history);

            //check if output path is not created then create it
            string outputPath = CreateAudioFile(userMessage.Message);
            //convert response to audio and save to file
            bool isTranscribed = await speechService.TextToSpeechAsync(assistantMessage.Text, outputPath);
            if (!isTranscribed)
            {
                return BadRequest("Failed to transcribe text to speech.");
            }
            // Get the relative path (e.g. "AudioTranscription/nomi/audio.mp3")
            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), outputPath)
                                    .Replace("\\", "/"); // Normalize for URLs

            // Save the audio file path to the DTO
            assistantMessage.AudioFilePath = relativePath;
            return Ok(assistantMessage);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserChatHistory>>> GetChatHistory()
        {
            List<UserChatHistoryDto> userChatHistory = new List<UserChatHistoryDto>();
            //first try to get from cache
            var chatHistory = await chatHandlerService.GetChatHistoryAsync(User.GetUsername());

            if (chatHandlerService != null)
            {
                foreach (var item in chatHistory)
                {
                    userChatHistory.Add(new UserChatHistoryDto()
                    {
                        ChatRole = item.Role.Label,
                        Message = item.Content,
                        MessageSent = DateTime.UtcNow
                    });
                }
            }

            //if not found in cache, get from database
            if (chatHistory == null || chatHistory.Count == 0)
            {
                var histories = await chatHistoryRepository.GetUserChatHistory(User.GetUserId());
                userChatHistory = mapper.Map<IEnumerable<UserChatHistoryDto>>(histories).ToList();
            }

            return Ok(userChatHistory);
        }

        private string CreateAudioFile(string userMessage)
        {
            // Use a consistent and safe base path — works on Azure and locally
            string basePath = Path.Combine(AppContext.BaseDirectory, "Data");

            var username = User.GetUsername();

            // Sanitize the user message for safe filename usage
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitizedMessage = new string(userMessage.ToLower()
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray());

            // Build full folder and file path
            var folderPath = Path.Combine(basePath, "AudioTranscription", username);
            var fileName = $"{sanitizedMessage}_{Guid.NewGuid()}_response.mp3";
            var responseAudioPath = Path.Combine(folderPath, fileName);

            Console.WriteLine($"Audio will be saved to: {responseAudioPath}");

            if (!Directory.Exists(folderPath))
            {
                // Create the directory if it doesn't exist
                Directory.CreateDirectory(folderPath);
            }

            // Optionally delete if somehow exists (super rare)
            if (System.IO.File.Exists(responseAudioPath))
            {
                System.IO.File.Delete(responseAudioPath);
            }

            return responseAudioPath;
        }
    }
}
