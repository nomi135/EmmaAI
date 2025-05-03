namespace API.Interfaces
{
    public interface ISpeechService
    {
        public Task<bool> TextToSpeechAsync(string text, string outputFilePath);
    }
}
