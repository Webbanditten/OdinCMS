using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class SettingsService : ISettingsService
    {
        private readonly DataContext _context;

        public SettingsService(DataContext context)
        {
            _context = context;
        }

        public async Task<bool> Exists(string setting)
        {
            var dbSetting = await this.Get(setting);
            return (dbSetting != null); 
        }

        public async Task<IEnumerable<Settings>> GetAll()
        {
            return await _context.Settings.ToListAsync();
        }

        public async Task<Settings> Get(string setting)
        {
            return await _context.Settings.Where(s => s.Setting.ToLower() == setting.ToLower()).FirstOrDefaultAsync();
        }

        public async Task<Settings> Update(Settings setting)
        {
            var dbSetting = await this.Get(setting.Setting);
            if(dbSetting != null) {
                dbSetting.Value = setting.Value;
                _context.Settings.Update(dbSetting);
                await _context.SaveChangesAsync();
            }

            return dbSetting;
        }

        public IEnumerable<DefaultSetting> GetDefaultSettings()
        {
            return new List<DefaultSetting> { 
                new DefaultSetting { Setting = "cms.use_ruffle", Value = "0", FriendlyName = "Use Ruffle to serve flash?" },
                new DefaultSetting { Setting = "cms.background", Value = "0", FriendlyName = "Background" },
                new DefaultSetting { Setting = "cms.hotel_banner", Value = "0", FriendlyName = "Hotel banner" }
            };
        }

        public async Task<IEnumerable<DefaultSetting>> GetMissingDefaultSettings()
        {
            var dbItems = await _context.Settings.ToListAsync();
            var defaultItems = this.GetDefaultSettings();
            var missingItems = defaultItems.Where(i => dbItems.All(s=>s.Setting != i.Setting));
            return missingItems;
        }

        public async Task<IEnumerable<Settings>> InstallDefaultSettings()
        {
            var missingSettings = await this.GetMissingDefaultSettings();
            var settingsToAdd = new List<Settings>();

            foreach(var item in missingSettings) {
                settingsToAdd.Add(new Settings { Setting = item.Setting, Value = item.Value });
            }

            await _context.Settings.AddRangeAsync(settingsToAdd);
            await _context.SaveChangesAsync();
            return settingsToAdd;
        }
    }

}
