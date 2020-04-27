using KeplerCMS.Data.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IUserService
    {
        public Users GetUserByUsername(string username);

        public Users GetUserById(string id);

        public void UpdateFigure(string userId, string figureData, string newGender);
    }
}
