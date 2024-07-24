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
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using MySql.Data.MySqlClient;

namespace KeplerCMS.Services.Implementations
{
    public class RoomChatlogsService : IRoomChatlogsService
    {
        private readonly DataContext _context;

        public RoomChatlogsService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<RoomChatlogs>> GetByUserId(int userId, int timestampLimiter, int take, int skip)
        {
            return await _context.RoomChatlogs.Where(s => s.UserId == userId && s.Timestamp < timestampLimiter).OrderByDescending(s => s.Timestamp).Skip(skip).Take(take)
                .Select(s => new RoomChatlogs
                {
                    Timestamp = s.Timestamp,
                    RoomId = s.RoomId,
                    UserId = s.UserId,
                    Message = s.Message,
                    ChatType = s.ChatType,
                    Username = s.User.Username,
                    RoomName = s.Room.Name,
                }).ToListAsync();
        }
        
        public async Task<List<RoomChatlogs>> GetByRoomId(int roomId, int timestampLimiter, int take, int skip)
        {
            return await _context.RoomChatlogs.Where(s => s.RoomId == roomId && s.Timestamp < timestampLimiter).OrderByDescending(s => s.Timestamp).Skip(skip).Take(take)
                .Select(s => new RoomChatlogs
                {
                    Timestamp = s.Timestamp,
                    RoomId = s.RoomId,
                    UserId = s.UserId,
                    Message = s.Message,
                    ChatType = s.ChatType,
                    Username = s.User.Username,
                    RoomName = s.Room.Name,
                }).ToListAsync();
        }
    }

}
