using API.Interfaces;
using Azure.Storage.Blobs;
using System.Text.RegularExpressions;

namespace API.Services
{
    public class FileService : IFileService
    {
        private readonly BlobContainerClient containerClient;
        private readonly string containerBaseUrl;
        private readonly string blobSasUrl;

        public FileService(IConfiguration config)
        {
            blobSasUrl = config.GetValue<string>("AzureBlobShare:BlobSASUrl") ?? throw new Exception("Blob SAS URL not found");
            containerClient = new BlobContainerClient(new Uri(blobSasUrl));
            containerBaseUrl = blobSasUrl.Split('?')[0]; // Just the base container URL
        }
        public async Task<string> CreateAudioFileAsync(string username, string text, byte[] audioBytes)
        {
            // Sanitize message
            var sanitizedText = Regex.Replace(text.ToLower(), @"[^a-z0-9]", "_").ToLower().Substring(0, Math.Min(text.Length, 10)); // Limit to 10 characters

            var fileName = $"{sanitizedText}_{Guid.NewGuid()}_response.mp3";
            var blobPath = $"AudioTranscription/{username}/{fileName}";

            var blobClient = containerClient.GetBlobClient(blobPath);

            using (var stream = new MemoryStream(audioBytes))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            // Reconstruct full URL with SAS token for frontend use
            return $"{containerBaseUrl}/{blobPath}{containerClient.Uri.Query}";
        }
    }
}
