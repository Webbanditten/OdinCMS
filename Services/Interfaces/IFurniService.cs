using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IFurniService
    {
        Task<List<ItemsDefinitions>> Search(string query);
        Task<ItemsDefinitions> Get(int id);
        Task<List<ItemsDefinitions>> Get(int[] ids);
    }
}
