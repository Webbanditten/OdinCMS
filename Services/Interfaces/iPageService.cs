using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IPageService
    {
        Task<Page> GetPageBySlug(string slug);
    }
}
