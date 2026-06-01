using System;
using KeplerCMS.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace KeplerCMS.Services.CampaignActions
{
    public interface ICampaignActionHandlerFactory
    {
        ICampaignActionHandler GetHandler(string actionType);
    }

    public class CampaignActionHandlerFactory : ICampaignActionHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CampaignActionHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICampaignActionHandler GetHandler(string actionType)
        {
            if (!Enum.TryParse<CampaignActionType>(actionType, true, out var type))
            {
                throw new ArgumentException($"Unknown campaign action type: {actionType}");
            }

            return type switch
            {
                CampaignActionType.ChangeBackground => _serviceProvider.GetRequiredService<BackgroundActionHandler>(),
                CampaignActionType.ChangeBanner => _serviceProvider.GetRequiredService<BannerActionHandler>(),
                CampaignActionType.CreateNews => _serviceProvider.GetRequiredService<NewsActionHandler>(),
                CampaignActionType.CataloguePageVisibility => _serviceProvider.GetRequiredService<CatalogueVisibilityActionHandler>(),
                CampaignActionType.RoomCcts => _serviceProvider.GetRequiredService<RoomCctsActionHandler>(),
                CampaignActionType.CatalogueRareItem => _serviceProvider.GetRequiredService<CatalogueRareItemActionHandler>(),
                _ => throw new ArgumentException($"No handler registered for action type: {actionType}")
            };
        }
    }
}
