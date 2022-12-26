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

        public async Task<Settings> Get(string setting, string fallback)
        {
            var dbSetting = await Get(setting);
            return dbSetting ?? new Settings { Setting = setting, Value = fallback };
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
                new DefaultSetting { Setting = "cms.figureUrl", Value = "/habbo-imaging/avatarimage", FriendlyName = "Path for Habbo avatars" },
                new DefaultSetting { Setting = "cms.badgeUrl", Value = "", FriendlyName = "Path for badge images" },
                new DefaultSetting { Setting = "cms.traxUrl", Value = "dcr.localhost/trax/mp3", FriendlyName = "Where to load mp3's for traxes from" },
                new DefaultSetting { Setting = "cms.groupBadgeUrl", Value = "", FriendlyName = "URL for generating group badges" },
                new DefaultSetting { Setting = "cms.port", Value = "0", FriendlyName = "Port for server" },
                new DefaultSetting { Setting = "cms.musport", Value = "0", FriendlyName = "Mus Port" },
                new DefaultSetting { Setting = "cms.host", Value = "0", FriendlyName = "Host" },
                new DefaultSetting { Setting = "cms.texts", Value = "0", FriendlyName = "Texts for hotel" },
                new DefaultSetting { Setting = "cms.vars", Value = "0", FriendlyName = "Variables for hotel" },
                new DefaultSetting { Setting = "cms.dcr", Value = "0", FriendlyName = "DCR file for hotel" },
                new DefaultSetting { Setting = "cms.hotel_banner", Value = "0", FriendlyName = "Hotel banner" },
                new DefaultSetting { Setting = "cms.maintenance", Value = "0", FriendlyName = "Enable maintenance" },
                new DefaultSetting { Setting = "allowed_username_chars", Value ="1234567890qwertyuiopasdfghjklzxcvbnm-=?!@:.æøå,+<>_", FriendlyName = "Allowed characters in usernames" },
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
