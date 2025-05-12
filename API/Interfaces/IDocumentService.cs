namespace API.Interfaces
{
    public interface IDocumentService
    {
        public Task<bool> SaveDocumentAsync(IFormFile file, string username);
        public Task<string> SearchDocumentAsync(string username, string userQuery);
    }
}
