using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.Services.Implementations
{
    /// <summary>
    /// Sends room photo requests to the room-photographer service (dirplayer-rs habbo/photographer) one at a time: each
    /// needs a fresh SSO ticket for the photographer account, and a new ticket replaces the previous one. The account
    /// must be Odin's room.photographer.username, so it gets into every room and nobody sees it.
    /// </summary>
    public class RoomPhotographer : BackgroundService, IRoomPhotographer
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan KeepFinished = TimeSpan.FromHours(1);

        private readonly IServiceProvider _services;
        private readonly ILogger<RoomPhotographer> _logger;
        private readonly string _url, _token, _username;
        private readonly ConcurrentDictionary<Guid, RoomPhotoRequest> _requests = new();
        private readonly Channel<RoomPhotoRequest> _queue = Channel.CreateUnbounded<RoomPhotoRequest>();
        private readonly object _lock = new();

        public RoomPhotographer(IServiceProvider services, IConfiguration configuration, ILogger<RoomPhotographer> logger)
        {
            _services = services;
            _logger = logger;
            var section = configuration.GetSection("keplercms:photographer");
            _url = (section["url"] ?? "").TrimEnd('/');
            _token = section["token"] ?? "";
            _username = section["username"] ?? "";
        }

        public bool Enabled => _url.Length > 0 && _username.Length > 0;

        public RoomPhotoRequest Request(int roomId, bool isPublicRoom, int requestedBy)
        {
            lock (_lock)
            {
                var pending = _requests.Values.FirstOrDefault(r => r.RoomId == roomId && (r.Status == "queued" || r.Status == "running"));
                if (pending != null) return pending;

                var request = new RoomPhotoRequest
                {
                    Id = Guid.NewGuid(), RoomId = roomId, IsPublicRoom = isPublicRoom, RequestedBy = requestedBy,
                    Status = "queued", RequestedAt = DateTime.Now,
                };
                _requests[request.Id] = request;
                _queue.Writer.TryWrite(request);
                return request;
            }
        }

        public RoomPhotoRequest Get(Guid id) => _requests.TryGetValue(id, out var request) ? request : null;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                RoomPhotoRequest request;
                try { request = await _queue.Reader.ReadAsync(stoppingToken); }
                catch (OperationCanceledException) { break; }

                request.Status = "running";
                try
                {
                    await Take(request, stoppingToken);
                    request.Status = "done";
                    _logger.LogInformation("Room photo of room {RoomId} saved", request.RoomId);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    request.Error = ex.Message;
                    request.Status = "failed";
                    _logger.LogWarning("Room photo of room {RoomId} failed: {Error}", request.RoomId, ex.Message);
                }
                request.FinishedAt = DateTime.Now;

                foreach (var old in _requests.Values.Where(r => r.FinishedAt < DateTime.Now - KeepFinished).ToList())
                    _requests.TryRemove(old.Id, out _);
            }
        }

        private HttpRequestMessage Call(HttpMethod method, string path)
        {
            var message = new HttpRequestMessage(method, _url + path);
            if (_token.Length > 0) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            return message;
        }

        private async Task Take(RoomPhotoRequest request, CancellationToken stoppingToken)
        {
            if (!Enabled) throw new InvalidOperationException("The room photographer isn't configured (keplercms:photographer)");

            string ticket;
            using (var scope = _services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<IUserService>();
                var photographer = await users.GetUserByUsername(_username)
                    ?? throw new InvalidOperationException($"The photographer account {_username} doesn't exist");
                ticket = (await users.GenerateSSO(photographer.Id)).SSOTicket;
            }

            using var create = Call(HttpMethod.Post, "/jobs");
            create.Content = JsonContent.Create(new { roomId = request.RoomId, roomType = request.IsPublicRoom ? "public" : "private", sso = ticket });
            using var created = await Http.SendAsync(create, stoppingToken);
            if (!created.IsSuccessStatusCode) throw new InvalidOperationException($"The photographer refused the job ({(int)created.StatusCode})");
            var jobId = (await created.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: stoppingToken)).GetProperty("id").GetString();

            var deadline = DateTime.Now + JobTimeout;
            while (true)
            {
                if (DateTime.Now > deadline) throw new TimeoutException("The photographer took too long");
                await Task.Delay(2000, stoppingToken);
                using var poll = Call(HttpMethod.Get, "/jobs/" + jobId);
                using var polled = await Http.SendAsync(poll, stoppingToken);
                if (polled.StatusCode == HttpStatusCode.NotFound) throw new InvalidOperationException("The photographer lost the job (restarted?)");
                if (!polled.IsSuccessStatusCode) continue;
                var job = await polled.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: stoppingToken);
                var status = job.GetProperty("status").GetString();
                if (status == "failed")
                    throw new InvalidOperationException(job.TryGetProperty("error", out var e) ? e.GetString() : "The photo failed");
                if (status != "done") continue;

                using var download = Call(HttpMethod.Get, "/jobs/" + jobId + "/photo.png");
                using var photo = await Http.SendAsync(download, stoppingToken);
                photo.EnsureSuccessStatusCode();
                var bytes = await photo.Content.ReadAsByteArrayAsync(stoppingToken);

                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                var row = await context.RoomPhotos.FirstOrDefaultAsync(p => p.RoomId == request.RoomId, stoppingToken);
                if (row == null)
                {
                    row = new RoomPhoto { RoomId = request.RoomId };
                    context.RoomPhotos.Add(row);
                }
                row.Photo = bytes;
                row.Width = job.GetProperty("width").GetInt32();
                row.Height = job.GetProperty("height").GetInt32();
                row.TakenAt = DateTime.Now;
                row.TakenBy = request.RequestedBy;
                await context.SaveChangesAsync(stoppingToken);
                return;
            }
        }
    }
}
