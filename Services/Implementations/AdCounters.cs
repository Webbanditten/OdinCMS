using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KeplerCMS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.Services.Implementations
{
    /// <summary>
    /// Ad views and clicks, counted in memory and added to cms_ads.views/clicks once a minute (and on shutdown), so a page
    /// view or an in-game ad download doesn't cost a database write.
    /// </summary>
    public class AdCounters : BackgroundService
    {
        private readonly ConcurrentDictionary<int, long[]> _pending = new();
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<AdCounters> _logger;

        public AdCounters(IServiceScopeFactory scopes, ILogger<AdCounters> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        public void View(int adId) => Interlocked.Increment(ref _pending.GetOrAdd(adId, _ => new long[2])[0]);

        public void Click(int adId) => Interlocked.Increment(ref _pending.GetOrAdd(adId, _ => new long[2])[1]);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); } catch (TaskCanceledException) { }
                await Flush();
            }
        }

        public async Task Flush()
        {
            if (_pending.IsEmpty) return;
            try
            {
                using var scope = _scopes.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                foreach (var id in _pending.Keys.ToList())
                {
                    if (!_pending.TryRemove(id, out var counts)) continue;
                    long views = Interlocked.Read(ref counts[0]), clicks = Interlocked.Read(ref counts[1]);
                    if (views == 0 && clicks == 0) continue;
                    await context.Ads.Where(a => a.Id == id).ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.Views, a => a.Views + views)
                        .SetProperty(a => a.Clicks, a => a.Clicks + clicks));
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Couldn't save ad counters");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await Flush();
        }
    }
}
