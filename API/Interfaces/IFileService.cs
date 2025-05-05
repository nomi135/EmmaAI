namespace API.Interfaces
{
    public interface IFileService
    {
        public Task<string> CreateAudioFileAsync(string username, string text, byte[] audioBytes);
    }
}
