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
    public class FuseService : IFuseService
    {
        private readonly DataContext _context;

        public FuseService(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<string>> GetFusesByRank(int rank)
        {
            return await _context.Fuses.Where(f => f.MinRank <= rank).Select(s=>s.Fuse.ToLower()).ToListAsync();
        }
    }

}
