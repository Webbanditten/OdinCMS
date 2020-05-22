using KeplerCMS.Data.Models;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IUserService
    {
        Task<Users> GetUserByUsername(string username);

        Task<Users> GetUserById(string id);

        public void UpdateFigure(string userId, string figureData, string newGender);
        public Task<Users> GenerateSSO(int userId);
    }
}
