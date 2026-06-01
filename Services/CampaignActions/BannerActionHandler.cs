using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;

namespace KeplerCMS.Services.CampaignActions
{
    public class BannerActionHandler : ICampaignActionHandler
    {
        private readonly ISettingsService _settingsService;
        private const string SettingKey = "cms.hotel_banner";
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public BannerActionHandler(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<string> TakeSnapshot(string actionData)
        {
            var currentSetting = await _settingsService.Get(SettingKey, "0");
            return JsonSerializer.Serialize(new BannerActionData { Value = currentSetting.Value });
        }

        public async Task Apply(string actionData)
        {
            var data = JsonSerializer.Deserialize<BannerActionData>(actionData, _jsonOptions);
            var setting = new Data.Models.Settings { Setting = SettingKey, Value = data.Value ?? "0" };
            await _settingsService.Update(setting);
        }

        public async Task Revert(string snapshotData)
        {
            var data = JsonSerializer.Deserialize<BannerActionData>(snapshotData, _jsonOptions);
            var setting = new Data.Models.Settings { Setting = SettingKey, Value = data.Value ?? "0" };
            await _settingsService.Update(setting);
        }

        public string GetConflictKey(string actionData)
        {
            return "setting:cms.hotel_banner";
        }
    }

    public class BannerActionData
    {
        public string Value { get; set; }
    }
}
