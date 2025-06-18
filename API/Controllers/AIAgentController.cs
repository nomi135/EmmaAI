using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace API.Controllers
{
    [Authorize]
    public class AIAgentController(IUnitOfWork unitOfWork, IChatHandlerService chatHandlerService, ISpeechService speechService,
                                   IDocumentService documentService, Kernel kernel, AzureOpenAIPromptExecutionSettings executionSettings, IMapper mapper) : BaseApiController
    {
        [HttpPost("chat")]
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

            var (result, intent) = await chatHandlerService.ProcessUserInputAsync(userMessage.Message, User.GetUsername());
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

            //convert response to audio and save to file
            string outputPath = await speechService.TextToSpeechAsync(User.GetUsername(), assistantMessage.Text);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return BadRequest("Failed to transcribe text to speech.");
            }

            // Save the audio file path to the DTO
            assistantMessage.AudioFilePath = outputPath;
            return Ok(assistantMessage);
        }

        [HttpPost("uploaddocument")]
        [RequestSizeLimit(5 * 1024 * 1024)] // Limit file size to 5 MB
        public async Task<ActionResult> UploadDocument([FromForm] IFormFile? file)
        {
            if (file == null)
            {
                return BadRequest("No document selected to upload.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Document size exceeds the 5 MB limit.");
            }

            var result = await documentService.SaveDocumentAsync(file, User.GetUsername());
            return Ok();
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
                var histories = await unitOfWork.UserChatHistoryRepository.GetUserChatHistoryAsync(User.GetUserId());
                userChatHistory = mapper.Map<IEnumerable<UserChatHistoryDto>>(histories).ToList();
            }

            return Ok(userChatHistory);
        }

    }
}
