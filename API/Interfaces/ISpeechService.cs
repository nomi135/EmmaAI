namespace API.Interfaces
{
    public interface ISpeechService
    {
        Task<string?> TextToSpeechAsync(string username, string text);
    }
}
