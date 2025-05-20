namespace API.Interfaces
{
    public interface IFileService
    {
        Task<string> CreateAudioFileAsync(string username, string text, byte[] audioBytes);
    }
}
