using System.Text.Json.Serialization;

namespace API.DTOs
{
    public class AssistantMessageDto
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string AudioFilePath { get; set; } = string.Empty;
    }
}
