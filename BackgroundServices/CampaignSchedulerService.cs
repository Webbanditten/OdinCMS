using System;
using System.Threading;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.BackgroundServices
{
    public class CampaignSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CampaignSchedulerService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

        public CampaignSchedulerService(
            IServiceProvider serviceProvider,
            ILogger<CampaignSchedulerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Campaign Scheduler Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckCampaigns();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking campaign schedules");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Campaign Scheduler Service stopped");
        }

        private async Task CheckCampaigns()
        {
            using var scope = _serviceProvider.CreateScope();
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var campaignExecutor = scope.ServiceProvider.GetRequiredService<ICampaignExecutor>();

            // Activate scheduled campaigns whose start date has passed
            var toActivate = await campaignService.GetScheduledCampaignsToActivate();
            foreach (var campaign in toActivate)
            {
                _logger.LogInformation("Auto-activating scheduled campaign {CampaignId} ({Name})", campaign.Id, campaign.Name);
                await campaignExecutor.ActivateCampaign(campaign.Id);
            }

            // Deactivate active campaigns whose end date has passed
            var toEnd = await campaignService.GetActiveCampaignsToEnd();
            foreach (var campaign in toEnd)
            {
                _logger.LogInformation("Auto-deactivating expired campaign {CampaignId} ({Name})", campaign.Id, campaign.Name);
                await campaignExecutor.DeactivateCampaign(campaign.Id);
            }
        }
    }
}
