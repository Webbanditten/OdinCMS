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
    public class RoomService : IRoomService
    {
        private readonly DataContext _context;

        public RoomService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Rooms>> GetRoomsByOwner(int ownerId)
        {
            return await _context.Rooms.Where(s => s.OwnerId == ownerId).ToListAsync();
        }
    }

}
