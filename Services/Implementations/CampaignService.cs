using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.Implementations
{
    public class CampaignService : ICampaignService
    {
        private readonly DataContext _context;

        public CampaignService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Campaign>> GetAll()
        {
            return await _context.Campaigns
                .Include(c => c.Actions)
                .Include(c => c.CreatedBy)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Campaign> GetById(int id)
        {
            return await _context.Campaigns
                .Include(c => c.Actions.OrderBy(a => a.OrderIndex))
                .Include(c => c.Snapshots)
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Campaign>> GetByStatus(string status)
        {
            return await _context.Campaigns
                .Include(c => c.Actions)
                .Where(c => c.Status == status)
                .ToListAsync();
        }

        public async Task<List<Campaign>> GetActiveCampaigns()
        {
            var activeStatus = CampaignStatus.Active.ToString().ToLower();
            return await _context.Campaigns
                .Include(c => c.Actions)
                .Include(c => c.Snapshots)
                .Where(c => c.Status == activeStatus)
                .ToListAsync();
        }

        public async Task<List<Campaign>> GetScheduledCampaignsToActivate()
        {
            var scheduledStatus = CampaignStatus.Scheduled.ToString().ToLower();
            var now = DateTime.Now;
            return await _context.Campaigns
                .Include(c => c.Actions)
                .Where(c => c.Status == scheduledStatus && c.StartDate.HasValue && c.StartDate.Value <= now)
                .ToListAsync();
        }

        public async Task<List<Campaign>> GetActiveCampaignsToEnd()
        {
            var activeStatus = CampaignStatus.Active.ToString().ToLower();
            var now = DateTime.Now;
            return await _context.Campaigns
                .Include(c => c.Actions)
                .Include(c => c.Snapshots)
                .Where(c => c.Status == activeStatus && c.EndDate.HasValue && c.EndDate.Value <= now)
                .ToListAsync();
        }

        public async Task<Campaign> Create(Campaign campaign)
        {
            campaign.CreatedAt = DateTime.Now;
            if (campaign.StartDate.HasValue && campaign.StartDate.Value > DateTime.Now)
            {
                campaign.StatusEnum = CampaignStatus.Scheduled;
            }
            else
            {
                campaign.StatusEnum = CampaignStatus.Draft;
            }

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();
            return campaign;
        }

        public async Task<Campaign> Update(Campaign campaign)
        {
            _context.Campaigns.Update(campaign);
            await _context.SaveChangesAsync();
            return campaign;
        }

        public async Task<bool> Delete(int id)
        {
            var campaign = await _context.Campaigns
                .Include(c => c.Actions)
                .Include(c => c.Snapshots)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null) return false;

            var activeStatus = CampaignStatus.Active.ToString().ToLower();
            if (campaign.Status == activeStatus)
                return false; // Cannot delete an active campaign

            _context.CampaignSnapshots.RemoveRange(campaign.Snapshots);
            _context.CampaignActions.RemoveRange(campaign.Actions);
            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Campaign> Clone(int campaignId, string newName, int createdById)
        {
            var source = await GetById(campaignId);
            if (source == null) return null;

            var clone = new Campaign
            {
                Name = newName,
                Description = source.Description,
                StartDate = null,
                EndDate = null,
                CreatedAt = DateTime.Now,
                CreatedById = createdById,
                ClonedFromId = source.Id
            };
            clone.StatusEnum = CampaignStatus.Draft;

            _context.Campaigns.Add(clone);
            await _context.SaveChangesAsync();

            // Clone actions
            foreach (var action in source.Actions)
            {
                var clonedAction = new CampaignAction
                {
                    CampaignId = clone.Id,
                    ActionType = action.ActionType,
                    ActionData = action.ActionData,
                    OrderIndex = action.OrderIndex
                };
                _context.CampaignActions.Add(clonedAction);
            }

            await _context.SaveChangesAsync();
            return await GetById(clone.Id);
        }

        public async Task UpdateStatus(int campaignId, string status)
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign != null)
            {
                campaign.Status = status;
                if (status == CampaignStatus.Active.ToString().ToLower())
                {
                    campaign.ActivatedAt = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<CampaignAction> AddAction(CampaignAction action)
        {
            _context.CampaignActions.Add(action);
            await _context.SaveChangesAsync();
            return action;
        }

        public async Task<bool> RemoveAction(int actionId)
        {
            var action = await _context.CampaignActions.FindAsync(actionId);
            if (action == null) return false;

            _context.CampaignActions.Remove(action);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CampaignAction> UpdateAction(CampaignAction action)
        {
            _context.CampaignActions.Update(action);
            await _context.SaveChangesAsync();
            return action;
        }

        public async Task<List<CampaignAction>> GetActions(int campaignId)
        {
            return await _context.CampaignActions
                .Where(a => a.CampaignId == campaignId)
                .OrderBy(a => a.OrderIndex)
                .ToListAsync();
        }

        public async Task SaveSnapshot(CampaignSnapshot snapshot)
        {
            snapshot.CreatedAt = DateTime.Now;
            _context.CampaignSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CampaignSnapshot>> GetSnapshots(int campaignId)
        {
            return await _context.CampaignSnapshots
                .Where(s => s.CampaignId == campaignId)
                .ToListAsync();
        }

        public async Task RemoveSnapshots(int campaignId)
        {
            var snapshots = await _context.CampaignSnapshots
                .Where(s => s.CampaignId == campaignId)
                .ToListAsync();

            _context.CampaignSnapshots.RemoveRange(snapshots);
            await _context.SaveChangesAsync();
        }
    }
}
