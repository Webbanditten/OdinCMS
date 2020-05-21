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

        public async Task<Settings> GetValue(string setting)
        {
            return await _context.Settings.Where(s => s.Setting.ToLower() == setting.ToLower()).FirstOrDefaultAsync();
        }
    }

}
