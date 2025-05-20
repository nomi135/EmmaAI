namespace API.Interfaces
{
    public interface IDocumentService
    {
        Task<bool> SaveDocumentAsync(IFormFile file, string username);
        Task<string> SearchDocumentAsync(string username, string userQuery);
    }
}
