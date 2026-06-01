using System;
using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;

namespace KeplerCMS.Services.CampaignActions
{
    public class NewsActionHandler : ICampaignActionHandler
    {
        private readonly INewsService _newsService;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public NewsActionHandler(INewsService newsService)
        {
            _newsService = newsService;
        }

        public Task<string> TakeSnapshot(string actionData)
        {
            // News creation doesn't have a "previous state" - we store the created article ID
            // so we can delete it on revert. Initially empty until after Apply.
            return Task.FromResult(JsonSerializer.Serialize(new NewsSnapshotData { CreatedNewsId = null }));
        }

        public async Task Apply(string actionData)
        {
            var data = JsonSerializer.Deserialize<NewsActionData>(actionData, _jsonOptions);
            var news = new News
            {
                Title = data.Title,
                Slug = data.Slug ?? GenerateSlug(data.Title),
                Teaser = data.Teaser,
                Text = data.Text,
                Writer = data.Writer ?? "System",
                PublishDate = DateTime.Now
            };
            await _newsService.Add(news);

            // Update the actionData with the created news ID for snapshot tracking
            data.CreatedNewsId = news.Id;
        }

        public async Task Revert(string snapshotData)
        {
            var data = JsonSerializer.Deserialize<NewsSnapshotData>(snapshotData, _jsonOptions);
            if (data.CreatedNewsId.HasValue)
            {
                await _newsService.Remove(data.CreatedNewsId.Value);
            }
        }

        public string GetConflictKey(string actionData)
        {
            // Each news article is unique - use a unique key based on slug
            var data = JsonSerializer.Deserialize<NewsActionData>(actionData, _jsonOptions);
            return $"news:{data.Slug ?? data.Title}";
        }

        private string GenerateSlug(string title)
        {
            return title?.ToLower()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("\"", "") ?? "campaign-news";
        }
    }

    public class NewsActionData
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Teaser { get; set; }
        public string Text { get; set; }
        public string Writer { get; set; }
        public int? CreatedNewsId { get; set; }
    }

    public class NewsSnapshotData
    {
        public int? CreatedNewsId { get; set; }
    }
}
