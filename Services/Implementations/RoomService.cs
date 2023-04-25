using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using MySql.Data.MySqlClient;

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

        public async Task<SearchRoomsModel> Search(string search, int take, int skip)
        {
            
           
            /*var allUsers = _context.Users
                .FromSqlRaw(
                    "SELECT * FROM users where (username like @letter AND username like @search) ORDER BY username ASC",
                    new MySqlParameter("@search", "%" + username + "%"),
                    new MySqlParameter("@letter", letter + "%"));
            var total = await allUsers.CountAsync();
            var users = await allUsers.OrderBy(u=>u.Username).Skip(skip).Take(take).ToListAsync();*/
            // Search either the name or the Owner.username 
            var allRooms = _context.Rooms
                .FromSqlRaw(
                    "SELECT * FROM rooms where (name like @search OR owner_id IN (SELECT id FROM users WHERE username like @search)) ORDER BY name ASC",
                    new MySqlParameter("@search", "%" + search + "%"));
            var total = await allRooms.CountAsync();
            var rooms = await allRooms.OrderBy(u => u.Name).Skip(skip).Take(take).ToListAsync();
            return new SearchRoomsModel
            {
                Rooms = rooms,
                TotalResults = total
            };
        }
    }

}
