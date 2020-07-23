using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IUserService
    {
        Task<Users> GetUserByUsername(string username);

        Task<Users> GetUserById(string id);

        public void UpdateFigure(string userId, string figureData, string newGender);
        public Task<Users> GenerateSSO(int userId);

        public Task<Users> Create(Users user);

        public Task<List<Tags>> Tags(int userId);

        public Task<Tags> AddTag(Tags tag);

        public void RemoveTag(Tags tag);


    }
}
