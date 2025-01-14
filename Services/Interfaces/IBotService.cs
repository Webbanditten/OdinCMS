using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Services.Interfaces
{
    public interface IBotService
    {
        public Task<List<Bots>> GetForRoom(int roomId);
        public Task<Bots> Get(int botId);
        public Task<List<Bots>> GetAllBots();
        public Task<bool> AddBot(Bots bot);
        public Task<bool> UpdateBot(Bots bot);
        public Task<bool> DeleteBot(int botId);
    }
}
