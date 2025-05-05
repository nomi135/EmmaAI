namespace API.Interfaces
{
    public interface ISpeechService
    {
        public Task<string?> TextToSpeechAsync(string username, string text);
    }
}
