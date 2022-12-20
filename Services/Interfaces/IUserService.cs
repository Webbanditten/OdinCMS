using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;

namespace KeplerCMS.Services.Interfaces
{
    public interface IUserService
    {
        Task<Users> GetUserByUsername(string username);

        Task<Users> GetUserById(int id);

        public void UpdateFigure(string userId, string figureData, string newGender);
        public Task<Users> GenerateSSO(int userId);

        public Task<Users> Create(Users user);

        public Task<Users> SetGroup(int userId, int groupId);
        public Task<Users[]> GetUsersByEmail(string email);

        public Task<string> GeneratePasswordResetLink(int userId);
        public Task<Users> ValidatePasswordResetLink(string guid);
        public Task<bool> ResetPassword(string guid, string password);
        public Task<UsersSearchModel> SearchUsers(string username, int take, int skip, string letter);
        public Task<IEnumerable<Users>> GetLatestSignins(int take, int skip);
        public Task<IEnumerable<Users>> GetLatestSignups(int take, int skip);
        public Task<int> GetMonthlySignups();
        public Task<int> TotalUsers();
        public Task<int> TotalSignedinUsers();

    }
}
