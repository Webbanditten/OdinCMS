using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
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
    }

}
