using Isopoh.Cryptography.Argon2;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using MySql.Data.MySqlClient;
using Westwind.Utilities;

namespace KeplerCMS.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly IFuseService _fuseService;
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public UserService(IConfiguration configuration, ICommandQueueService commandQueueService, IFuseService fuseService, DataContext context)
        {
            _commandQueueService = commandQueueService;
            _fuseService = fuseService;
            _context = context;
            _configuration = configuration;
        }

        public async Task<Users> GetUserByUsername(string username)
        {
            var user = await _context.Users.Where(user => user.Username.ToLower() == username.ToLower()).FirstOrDefaultAsync();
            if(user != null)
            {
                user.Fuses = await _fuseService.GetFusesByRank(user.Rank, user.HasHabboClub);
            }
            return user;
        }

        public async Task<Users> GetUserById(int id)
        {
            var user = await _context.Users.Where(user => user.Id == id).FirstOrDefaultAsync();
            if (user != null)
            {
                user.Fuses = await _fuseService.GetFusesByRank(user.Rank, user.HasHabboClub);
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

        public Task<Users[]> GetUsersByEmail(string email)
        {
            return _context.Users.Where(user => user.Email.ToLower() == email.ToLower()).ToArrayAsync();
        }

        public async Task<string> GeneratePasswordResetLink(int userId)
        {
            var user = await GetUserById(userId);
            if(user != null)
            {
                var guid = Guid.NewGuid().ToString();
                var timestamp = DateTime.Now;
                var resetPassword = new ResetPassword
                {
                    UserId = userId,
                    Timestamp = timestamp,
                    guid = guid
                };
                _context.ResetPasswords.Add(resetPassword);
                await _context.SaveChangesAsync();
                return string.IsNullOrEmpty(_configuration.GetSection("keplercms:publicUrl").Value) ? "http://localhost:5000/account/forgot/reset/"+guid : _configuration.GetSection("keplercms:publicUrl").Value+"/account/forgot/reset/"+guid;
            }
            return null;
        }

        public async Task<Users> ValidatePasswordResetLink(string guid)
        {
            var resetPasswordEntry = await _context.ResetPasswords.Where(reset => reset.guid == guid).FirstOrDefaultAsync();
            if(resetPasswordEntry != null && resetPasswordEntry.Timestamp.AddMinutes(10) >= DateTime.Now) {
                return await GetUserById(resetPasswordEntry.UserId);
            }
            return null;
        }

        public async Task<bool> ResetPassword(string guid, string password)
        {
            var resetPasswordEntry = await _context.ResetPasswords.Where(reset => reset.guid == guid).FirstOrDefaultAsync();
            if(resetPasswordEntry != null) {
                var user = await ValidatePasswordResetLink(guid);
                _context.ResetPasswords.Remove(resetPasswordEntry);
                if(user != null) {
                    user.Password = Argon2.Hash(password);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    return true;
                }
                await _context.SaveChangesAsync();
            }

            return false;
        }

        public async Task<UsersSearchModel> SearchUsers(string username, int take, int skip, string letter)
        {
            if (username != null)
            {
                if(letter != null)
                {
                    var allUsers = _context.Users
                        .FromSqlRaw(
                            "SELECT * FROM users where (username like @letter AND username like @search) ORDER BY username ASC",
                            new MySqlParameter("@search", "%" + username + "%"),
                            new MySqlParameter("@letter", letter + "%"));
                    var total = await allUsers.CountAsync();
                    var users = await allUsers.OrderBy(u=>u.Username).Skip(skip).Take(take).ToListAsync();
                    return new UsersSearchModel { Users = users, TotalResults = total };
                }
                else
                {
                    var allUsers = _context.Users.Where(user => user.Username.ToLower().Contains(username.ToLower()))
                        .OrderBy(user => user.Username);
                    var total = await allUsers.CountAsync();
                    var users = await allUsers.OrderBy(u=>u.Username).Skip(skip).Take(take).ToListAsync();
                    return new UsersSearchModel { Users = users, TotalResults = total };
                }
            }
            if (letter != null)
            {
                var allUsers = _context.Users.FromSqlRaw(
                    "SELECT * FROM users where username like @letter ORDER BY username ASC",
                    new MySqlParameter("@letter", letter + "%"));
                var total = await allUsers.CountAsync();
                var users = await allUsers.OrderBy(u=>u.Username).Skip(skip).Take(take).ToListAsync();
                return new UsersSearchModel { Users = users, TotalResults = total };
            }
            else
            {
                var allUsers = _context.Users.OrderBy(user => user.Username);
                var total = await allUsers.CountAsync();
                var users = await allUsers.OrderBy(u=>u.Username).Skip(skip).Take(take).ToListAsync();
                return new UsersSearchModel { Users = users, TotalResults = total };
            }
        }

        public async Task<IEnumerable<Users>> GetLatestSignins(int take, int skip)
        {
            return await _context.Users.OrderByDescending(u => u.LastOnlineTimestamp).Take(take).Skip(skip).ToListAsync();
        }

        public async Task<IEnumerable<Users>> GetLatestSignups(int take, int skip)
        {
            return await _context.Users.OrderByDescending(u => u.CreateAt).Take(take).Skip(skip).ToListAsync();
        }

        public async Task<int> GetMonthlySignups()
        {
            var previousMonth = DateTime.Now.AddMonths(-1);
            return await _context.Users.Where(user => user.CreateAt >= previousMonth).CountAsync();
        }

        public async Task<int> TotalUsers()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> TotalSignedinUsers()
        {
            return await _context.Users.Where(user => user.LastOnlineTimestamp != 0).CountAsync();
        }

        public async Task<Users> Update(Users user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<IEnumerable<Users>> GetUserByRank(int id)
        {
            return await  _context.Users.Where(user => user.Rank == id).ToListAsync();
        }
    }

}
