using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IUploadService
    {
        List<UploadViewModel> GetAll();
        Task<Upload> GetByCategoryAndName(string category, string name);
        Task<Upload> GetByCategoryAndNameNoBlob(string category, string name);
        Task<UploadUpdateViewModel> GetWithoutBlob(int id);
        Task<bool> Add(UploadAddViewModel model);
        Task<bool> Remove(int id);
        Task<Upload> Update(UploadUpdateViewModel model);
    }
}
