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
    public class UserService : IUserService
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly DataContext _context;

        public UserService(ICommandQueueService commandQueueService, DataContext context)
        {
            _commandQueueService = commandQueueService;
            _context = context;
        }

        public async Task<Users> GetUserByUsername(string username)
        {
            return await _context.Users.Where(user => user.Username.ToLower() == username.ToLower()).FirstOrDefaultAsync();
        }

        public async Task<Users> GetUserById(string id)
        {
            return await _context.Users.Where(user => user.Id == int.Parse(id)).FirstOrDefaultAsync();
        }

        public void UpdateFigure(string userId, string figureData, string newGender)
        {
            var user = _context.Users.Where(u => u.Id == int.Parse(userId)).FirstOrDefault();
            user.Figure = FigureHelper.FixFigure(figureData);
            user.Gender = newGender;
            _context.SaveChanges();

            _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.refresh_appearance, userId);
        }
    }

}
