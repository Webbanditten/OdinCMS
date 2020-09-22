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
    public class UserService : IUserService
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly IFuseService _fuseService;
        private readonly DataContext _context;

        public UserService(ICommandQueueService commandQueueService, IFuseService fuseService, DataContext context)
        {
            _commandQueueService = commandQueueService;
            _fuseService = fuseService;
            _context = context;
        }

        public async Task<Users> GetUserByUsername(string username)
        {
            var user = await _context.Users.Where(user => user.Username.ToLower() == username.ToLower()).FirstOrDefaultAsync();
            if(user != null)
            {
                user.Fuses = await _fuseService.GetFusesByRank(user.Rank);
            }
            return user;
        }

        public async Task<Users> GetUserById(int id)
        {
            var user = await _context.Users.Where(user => user.Id == id).FirstOrDefaultAsync();
            if (user != null)
            {
                user.Fuses = await _fuseService.GetFusesByRank(user.Rank);
            }
            return user;
        }

        public void UpdateFigure(string userId, string figureData, string newGender)
        {
            var user = _context.Users.Where(u => u.Id == int.Parse(userId)).FirstOrDefault();
            //user.Figure = FigureHelper.FixFigure(figureData);
            user.Figure = figureData;
            user.Gender = newGender;
            _context.SaveChanges();

            _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.refresh_appearance, new CommandTemplate { UserId = user.Id });
        }

        public async Task<Users> GenerateSSO(int userId)
        {
            var user = await GetUserById(userId);
            if(user != null)
            {
                user.SSOTicket = Guid.NewGuid().ToString();
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
            return user;
        }

        public async Task<Users> Create(Users user)
        {
            var existingUser = await GetUserByUsername(user.Username);
            if(existingUser == null)
            {
                user.CreateAt = DateTime.Now;
                user.Badge = "";
                user.Motto = "";
                user.Password = Argon2.Hash(user.Password);
                user.Credits = 200;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
            return null;
        }

        public async Task<Users> SetGroup(int userId, int groupId)
        {
            var user = await GetUserById(userId);
            if(user != null)
            {
                user.Group = groupId;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
            return user;
        }
    }

}
