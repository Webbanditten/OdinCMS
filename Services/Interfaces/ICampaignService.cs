using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICampaignService
    {
        Task<List<Campaign>> GetAll();
        Task<Campaign> GetById(int id);
        Task<List<Campaign>> GetByStatus(string status);
        Task<List<Campaign>> GetActiveCampaigns();
        Task<List<Campaign>> GetScheduledCampaignsToActivate();
        Task<List<Campaign>> GetActiveCampaignsToEnd();
        Task<Campaign> Create(Campaign campaign);
        Task<Campaign> Update(Campaign campaign);
        Task<bool> Delete(int id);
        Task<Campaign> Clone(int campaignId, string newName, int createdById);
        Task UpdateStatus(int campaignId, string status);
        Task<CampaignAction> AddAction(CampaignAction action);
        Task<bool> RemoveAction(int actionId);
        Task<CampaignAction> UpdateAction(CampaignAction action);
        Task<List<CampaignAction>> GetActions(int campaignId);
        Task SaveSnapshot(CampaignSnapshot snapshot);
        Task<List<CampaignSnapshot>> GetSnapshots(int campaignId);
        Task RemoveSnapshots(int campaignId);
    }
}
