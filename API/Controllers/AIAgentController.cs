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
    public class AIAgentController(IUserChatHistoryRepository chatHistoryRepository, IChatHandler chatHandler, Kernel kernel,
                                   AzureOpenAIPromptExecutionSettings executionSettings, IMapper mapper) : BaseApiController
    {
        [HttpPost]
        public async Task<ActionResult<AssistantMessageDto>> Chat([FromBody] UserMessageDto userMessage)
        {
            ChatHistory history = new ChatHistory();

            var chatHistory = await chatHandler.GetChatHistoryAsync(User.GetUsername());
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

            var response = await chatHandler.ProcessUserInputAsync(userMessage.Message);
            var textItem = new TextContent(response.result);
            if (string.IsNullOrEmpty(response.result))
            {
                if (!string.IsNullOrWhiteSpace(response.intent.ResponseStyle))
                {
                    history.AddSystemMessage($"Detected emotion:{response.intent.Emotion} reply in: {response.intent.ResponseStyle} tone.");
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
            string cache = await chatHandler.SaveChatHistoryAsync(User.GetUsername(), history);

            return Ok(assistantMessage);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserChatHistory>>> GetChatHistory()
        {
            List<UserChatHistoryDto> userChatHistory = new List<UserChatHistoryDto>();
            //first try to get from cache
            var chatHistory = await chatHandler.GetChatHistoryAsync(User.GetUsername());

            if (chatHandler != null)
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
    }
}
