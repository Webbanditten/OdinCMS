using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IPhotoService
    {
        Task<ItemsPhotos> Get(int id);
    }
}
