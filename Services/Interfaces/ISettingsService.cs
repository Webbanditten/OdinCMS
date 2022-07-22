using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ISettingsService
    {
        Task<bool> Exists(string setting);
        Task<IEnumerable<Settings>> GetAll();
        
        Task<Settings> Update(Settings setting);
        Task<Settings> Get(string setting);
        IEnumerable<DefaultSetting> GetDefaultSettings();
        Task<IEnumerable<DefaultSetting>> GetMissingDefaultSettings();
        Task<IEnumerable<Settings>> InstallDefaultSettings();

    }
}
