using API.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using API.DTOs;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using DocumentFormat.OpenXml.Packaging;

namespace API.Services
{
    public class DocumentService(Kernel kernel, IMemoryCache cache) : IDocumentService
    {
        // Cache expiration period – adjust as needed.
        MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(1));
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly ITextEmbeddingGenerationService embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        [Description("Save document to memory cache")]
        public async Task<bool> SaveDocumentAsync(IFormFile file, string username)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var fileBytes = stream.ToArray();

            var fileMeta = new CachedFileDto
            {
                Content = fileBytes,
                ContentType = file.ContentType,
                FileName = file.FileName
            };

            string fileUrlCacheKey = $"document_search_{username}";
            var result = cache.Set(fileUrlCacheKey, fileMeta, cacheEntryOptions);
            return await Task.FromResult(true);
        }

        public async Task<string> SearchDocumentAsync(string username, string userQuery)
        {
            string fileUrlCacheKey = $"document_search_{username}";
            if (!cache.TryGetValue(fileUrlCacheKey, out CachedFileDto cachedFile))
            {
                return "No document found. Please upload document and then ask question";
            }
            var ext = Path.GetExtension(cachedFile.FileName).ToLowerInvariant();

            string content = string.Empty;
            switch (ext)
            {
                case ".txt":
                    content = Encoding.UTF8.GetString(cachedFile.Content);
                    break;
                case ".pdf":
                    content = ExtractTextFromPdf(cachedFile.Content);
                    break;
                case ".docx":
                    return ExtractTextFromWord(cachedFile.Content);

                // You can add support for .xlsx, .csv, .rtf, etc.
                default:
                    return "Unsupported document type";
            }

            var chunks = SplitIntoChunks(content, 500);
            var chunkEmbeddings = await embeddingService.GenerateEmbeddingsAsync(chunks);
            var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(userQuery);

            // Calculate cosine similarity between query and all chunks
            var scoredChunks = chunks.Zip(chunkEmbeddings, (chunk, embedding) => new
            {
                Chunk = chunk,
                Score = CosineSimilarity(queryEmbedding.ToArray(), embedding.ToArray())
            });

            var topChunk = scoredChunks.OrderByDescending(c => c.Score).First();

            // Use top chunk in a prompt to LLM to generate final answer
            string prompt = $"You are a document search assistant. Use this document to answer the user question:\n\n" +
                            $"Document:\n\"{topChunk.Chunk}\"\n\n" +
                            $"Question: \"{userQuery}\"\n\nAnswer:";

            var result = await kernel.InvokePromptAsync(prompt);
            return result.GetValue<string>() ?? "No relevant answer found.";
        }

        private string ExtractTextFromPdf(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            var sb = new StringBuilder();
            using (PdfReader reader = new PdfReader(stream))
            {
                using (PdfDocument document = new PdfDocument(reader))
                {
                    for (int page = 1; page <= document.GetNumberOfPages(); page++)
                    {
                        var strategy = new LocationTextExtractionStrategy(); // preserves layout
                        string pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(page), strategy);

                        sb.AppendLine($"--- Page {page} ---");
                        sb.AppendLine(pageText);
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }
        private string ExtractTextFromWord(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var doc = WordprocessingDocument.Open(stream, false);
            return doc.MainDocumentPart?.Document?.InnerText ?? string.Empty;
        }

        private List<string> SplitIntoChunks(string text, int maxTokens)
        {
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            var chunks = new List<string>();
            var sb = new StringBuilder();

            int tokenCount = 0;
            foreach (var sentence in sentences)
            {
                tokenCount += sentence.Length / 4; // rough token estimate

                if (tokenCount > maxTokens)
                {
                    chunks.Add(sb.ToString().Trim());
                    sb.Clear();
                    tokenCount = sentence.Length / 4;
                }

                sb.Append(sentence + " ");
            }

            if (sb.Length > 0)
                chunks.Add(sb.ToString().Trim());

            return chunks;
        }

        private static double CosineSimilarity(IList<float> v1, IList<float> v2)
        {
            double dot = 0.0;
            double mag1 = 0.0;
            double mag2 = 0.0;

            for (int i = 0; i < v1.Count; i++)
            {
                dot += v1[i] * v2[i];
                mag1 += Math.Pow(v1[i], 2);
                mag2 += Math.Pow(v2[i], 2);
            }

            return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
        }
    }
}
