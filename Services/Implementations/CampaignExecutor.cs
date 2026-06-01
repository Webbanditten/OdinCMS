using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.CampaignActions;
using KeplerCMS.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.Services.Implementations
{
    public class CampaignExecutor : ICampaignExecutor
    {
        private readonly ICampaignService _campaignService;
        private readonly ICampaignActionHandlerFactory _handlerFactory;
        private readonly ICommandQueueService _commandQueueService;
        private readonly ILogger<CampaignExecutor> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public CampaignExecutor(
            ICampaignService campaignService,
            ICampaignActionHandlerFactory handlerFactory,
            ICommandQueueService commandQueueService,
            ILogger<CampaignExecutor> logger)
        {
            _campaignService = campaignService;
            _handlerFactory = handlerFactory;
            _commandQueueService = commandQueueService;
            _logger = logger;
        }

        public async Task ActivateCampaign(int campaignId)
        {
            var campaign = await _campaignService.GetById(campaignId);
            if (campaign == null)
            {
                _logger.LogWarning("Campaign {CampaignId} not found", campaignId);
                return;
            }

            if (campaign.StatusEnum == CampaignStatus.Active)
            {
                _logger.LogWarning("Campaign {CampaignId} is already active", campaignId);
                return;
            }

            var activeCampaigns = await _campaignService.GetActiveCampaigns();
            bool needsCatalogueReload = false;
            bool needsNavigatorReload = false;

            foreach (var action in campaign.Actions.OrderBy(a => a.OrderIndex))
            {
                try
                {
                    var handler = _handlerFactory.GetHandler(action.ActionType);
                    var conflictKey = handler.GetConflictKey(action.ActionData);

                    // Check if another active campaign already manages this setting
                    string originalData;
                    var existingSnapshot = activeCampaigns
                        .SelectMany(c => c.Snapshots)
                        .FirstOrDefault(s => s.ConflictKey == conflictKey);

                    if (existingSnapshot != null)
                    {
                        // Preserve the true original (pre-any-campaign) value
                        originalData = existingSnapshot.OriginalData;
                    }
                    else
                    {
                        // No other campaign manages this setting, snapshot current DB state
                        originalData = await handler.TakeSnapshot(action.ActionData);
                    }

                    // Save the snapshot
                    var snapshot = new CampaignSnapshot
                    {
                        CampaignId = campaign.Id,
                        ActionId = action.Id,
                        ConflictKey = conflictKey,
                        OriginalData = originalData
                    };
                    await _campaignService.SaveSnapshot(snapshot);

                    // Apply the action
                    await handler.Apply(action.ActionData);

                    // Track what needs reloading
                    if (action.ActionType == CampaignActionType.CataloguePageVisibility.ToString())
                        needsCatalogueReload = true;
                    if (action.ActionType == CampaignActionType.RoomCcts.ToString())
                        needsNavigatorReload = true;

                    // For news, update the snapshot with the created article ID
                    if (action.ActionType == CampaignActionType.CreateNews.ToString())
                    {
                        var newsData = JsonSerializer.Deserialize<NewsActionData>(action.ActionData, _jsonOptions);
                        if (newsData.CreatedNewsId.HasValue)
                        {
                            snapshot.OriginalData = JsonSerializer.Serialize(
                                new NewsSnapshotData { CreatedNewsId = newsData.CreatedNewsId });
                            await _campaignService.SaveSnapshot(snapshot);
                        }
                    }

                    _logger.LogInformation("Applied action {ActionType} for campaign {CampaignId}", action.ActionType, campaignId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply action {ActionId} ({ActionType}) for campaign {CampaignId}",
                        action.Id, action.ActionType, campaignId);
                }
            }

            // Update campaign status
            await _campaignService.UpdateStatus(campaignId, CampaignStatus.Active.ToString().ToLower());

            // Send server reload commands
            SendReloadCommands(needsCatalogueReload, needsNavigatorReload);

            _logger.LogInformation("Campaign {CampaignId} ({Name}) activated successfully", campaignId, campaign.Name);
        }

        public async Task DeactivateCampaign(int campaignId)
        {
            var campaign = await _campaignService.GetById(campaignId);
            if (campaign == null)
            {
                _logger.LogWarning("Campaign {CampaignId} not found", campaignId);
                return;
            }

            if (campaign.StatusEnum != CampaignStatus.Active)
            {
                _logger.LogWarning("Campaign {CampaignId} is not active (status: {Status})", campaignId, campaign.Status);
                return;
            }

            var activeCampaigns = await _campaignService.GetActiveCampaigns();
            var otherActiveCampaigns = activeCampaigns.Where(c => c.Id != campaignId).ToList();

            bool needsCatalogueReload = false;
            bool needsNavigatorReload = false;

            // Process actions in reverse order for clean rollback
            foreach (var action in campaign.Actions.OrderByDescending(a => a.OrderIndex))
            {
                try
                {
                    var handler = _handlerFactory.GetHandler(action.ActionType);
                    var conflictKey = handler.GetConflictKey(action.ActionData);

                    // Check if another active campaign also targets this setting
                    var conflictingCampaign = otherActiveCampaigns
                        .Where(c => c.Actions.Any(a =>
                        {
                            var h = _handlerFactory.GetHandler(a.ActionType);
                            return h.GetConflictKey(a.ActionData) == conflictKey;
                        }))
                        .OrderByDescending(c => c.ActivatedAt)
                        .FirstOrDefault();

                    if (conflictingCampaign != null)
                    {
                        // Another campaign is active for this setting - apply its desired value
                        var conflictingAction = conflictingCampaign.Actions
                            .First(a =>
                            {
                                var h = _handlerFactory.GetHandler(a.ActionType);
                                return h.GetConflictKey(a.ActionData) == conflictKey;
                            });
                        await handler.Apply(conflictingAction.ActionData);
                        _logger.LogInformation(
                            "Conflict on {ConflictKey}: fell back to campaign {FallbackId} value",
                            conflictKey, conflictingCampaign.Id);
                    }
                    else
                    {
                        // No other campaign - restore the original value from snapshot
                        var snapshot = campaign.Snapshots.FirstOrDefault(s => s.ActionId == action.Id);
                        if (snapshot != null)
                        {
                            await handler.Revert(snapshot.OriginalData);
                        }
                        else
                        {
                            _logger.LogWarning("No snapshot found for action {ActionId} in campaign {CampaignId}",
                                action.Id, campaignId);
                        }
                    }

                    // Track what needs reloading
                    if (action.ActionType == CampaignActionType.CataloguePageVisibility.ToString())
                        needsCatalogueReload = true;
                    if (action.ActionType == CampaignActionType.RoomCcts.ToString())
                        needsNavigatorReload = true;

                    _logger.LogInformation("Reverted action {ActionType} for campaign {CampaignId}", action.ActionType, campaignId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to revert action {ActionId} ({ActionType}) for campaign {CampaignId}",
                        action.Id, action.ActionType, campaignId);
                }
            }

            // Update campaign status
            await _campaignService.UpdateStatus(campaignId, CampaignStatus.Ended.ToString().ToLower());

            // Clean up snapshots
            await _campaignService.RemoveSnapshots(campaignId);

            // Send server reload commands
            SendReloadCommands(needsCatalogueReload, needsNavigatorReload);

            _logger.LogInformation("Campaign {CampaignId} ({Name}) deactivated successfully", campaignId, campaign.Name);
        }

        private void SendReloadCommands(bool catalogue, bool navigator)
        {
            if (catalogue)
            {
                _commandQueueService.QueueCommand(CommandQueueType.reload_catalogue, new CommandTemplate());
            }
            if (navigator)
            {
                _commandQueueService.QueueCommand(CommandQueueType.reload_navigator, new CommandTemplate());
            }
        }
    }
}
