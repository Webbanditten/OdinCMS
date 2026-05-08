using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.Implementations
{
    public class CallsForHelpService : ICallsForHelpService
    {
        private readonly DataContext _context;

        public CallsForHelpService(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CallsForHelp>> GetByDate(DateTime date)
        {
            return await _context.CallsForHelp
                .Where(c => c.CreatedAt.Date == date.Date)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CallsForHelp>> GetPending()
        {
            return await _context.CallsForHelp
                .Where(c => c.PickedUpBy == 0)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
