using Isopoh.Cryptography.Argon2;
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
    public class FurniService : IFurniService
    {
        private readonly DataContext _context;
        public FurniService(DataContext context, IUserService userService)
        {
            _context = context;
        }

        public async Task<List<ItemsDefinitions>> Search(string query)
        {
            return await _context.ItemsDefinitions.Where(x => x.sprite.Contains(query) || x.Name.Contains(query)).ToListAsync();
        }

        public async Task<ItemsDefinitions> Get(int id)
        {
            return await _context.ItemsDefinitions.Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<ItemsDefinitions>> Get(int[] ids)
        {
            return await _context.ItemsDefinitions.Where(x => ids.Contains(x.Id)).ToListAsync();
        }
    }
}
