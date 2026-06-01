using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;

namespace KeplerCMS.Services.CampaignActions
{
    public class BackgroundActionHandler : ICampaignActionHandler
    {
        private readonly ISettingsService _settingsService;
        private const string SettingKey = "cms.background";
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public BackgroundActionHandler(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<string> TakeSnapshot(string actionData)
        {
            var currentSetting = await _settingsService.Get(SettingKey, "0");
            return JsonSerializer.Serialize(new BackgroundActionData { Value = currentSetting.Value });
        }

        public async Task Apply(string actionData)
        {
            var data = JsonSerializer.Deserialize<BackgroundActionData>(actionData, _jsonOptions);
            var setting = new Data.Models.Settings { Setting = SettingKey, Value = data.Value ?? "0" };
            await _settingsService.Update(setting);
        }

        public async Task Revert(string snapshotData)
        {
            var data = JsonSerializer.Deserialize<BackgroundActionData>(snapshotData, _jsonOptions);
            var setting = new Data.Models.Settings { Setting = SettingKey, Value = data.Value ?? "0" };
            await _settingsService.Update(setting);
        }

        public string GetConflictKey(string actionData)
        {
            return "setting:cms.background";
        }
    }

    public class BackgroundActionData
    {
        public string Value { get; set; }
    }
}
