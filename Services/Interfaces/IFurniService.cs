using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IFurniService
    {
        Task<List<ItemsDefinitions>> Search(string query);
        Task<ItemsDefinitions> Get(int id);
        Task<List<ItemsDefinitions>> Get(int[] ids);
        Task<PopularFurniContainer> GetPopularFurni();
    }
}
