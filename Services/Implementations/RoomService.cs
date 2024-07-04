﻿using KeplerCMS.Data;
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
            /*var roomsQuery = from room in _context.Rooms.Include(r => r.Owner)
                where room.Name.Contains(search) || _context.Users.Any(u => u.Id == room.OwnerId && u.Username.Contains(search))
                orderby room.Name ascending
                select room;

            var total = await roomsQuery.CountAsync();
            var rooms = await roomsQuery.Skip(skip).Take(take).ToListAsync();

            return new SearchRoomsModel
            {
                Rooms = rooms,
                TotalResults = total
            };*/
            
            var roomsQuery = from room in _context.Rooms
                join owner in _context.Users on room.OwnerId equals owner.Id into owners
                from owner in owners.DefaultIfEmpty()
                where (room.Name.Contains(search) || (owner != null && owner.Username.Contains(search))) && room.Model.Contains("model_")
                orderby room.Name ascending
                select new { Room = room, Owner = owner };

            var total = await roomsQuery.CountAsync();
            var rooms = await roomsQuery.Skip(skip).Take(take).Select(joinResult => joinResult.Room).ToListAsync();

            foreach (var room in rooms)
            {
                room.Owner = roomsQuery.Where(joinResult => joinResult.Room == room).Select(joinResult => joinResult.Owner).FirstOrDefault();
            }

            return new SearchRoomsModel
            {
                Rooms = rooms,
                TotalResults = total
            };
        }

        public async Task<string> UpdateRoom(RoomsUpdateModel model)
        {
            try
            {
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == model.Id);
                if (room == null) return "error";
                room.Name = model.Name;
                room.Description = model.Description;
                room.ShowOwner = model.ShowOwner;
                room.AccessType = model.AccessType;
            
                await _context.SaveChangesAsync();
                
                return "success";
            } catch (Exception e)
            {
                return "error";
            }
            
        }
    }

}
