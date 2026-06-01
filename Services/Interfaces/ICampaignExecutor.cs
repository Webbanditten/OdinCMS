using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICampaignExecutor
    {
        Task ActivateCampaign(int campaignId);
        Task DeactivateCampaign(int campaignId);
    }
}
