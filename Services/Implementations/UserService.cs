using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class UserService : IUserService
    {
        private ICommandQueueService _commandQueueService;
        private DataContext _context;

        public UserService(ICommandQueueService commandQueueService, DataContext context)
        {
            _commandQueueService = commandQueueService;
            _context = context;
        }

        public Users GetUserByUsername(string username)
        {
            return _context.Users.Where(user => user.Username.ToLower() == username.ToLower()).FirstOrDefault();
        }

        public Users GetUserById(string id)
        {
            return _context.Users.Where(user => user.Id == int.Parse(id)).FirstOrDefault();
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
