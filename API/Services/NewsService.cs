using API.Interfaces;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace API.Services
{
    public class NewsService(HttpClient httpClient) : INewsService
    {
        [Description("Gets the latest news")]
        public async Task<string?> GetLatestNewsAsync(string country)
        {
            var apiKey = "pub_781081afc5776bc91cbfc1902dc6123072c17";
            var url = $"https://newsdata.io/api/1/latest?apikey={apiKey}&country={country}&prioritydomain=top&category=top&language=en&removeduplicate=1";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return "Failed to fetch news data";
            }
            var content = await response.Content.ReadAsStringAsync();
            var newsData = System.Text.Json.JsonDocument.Parse(content);
            var articles = newsData.RootElement.GetProperty("results");
            var newsList = new List<string>();
            foreach (var article in articles.EnumerateArray())
            {
                var title = article.GetProperty("title").GetString();
                newsList.Add($"{title}");
                //var link = article.GetProperty("link").GetString();
                //var description = article.GetProperty("description").GetString();
                //newsList.Add($"Title: {title}\nLink: {link}\nDescription: {description}");
            }
            return string.Join("\n\n", newsList);
        }
    }
}
