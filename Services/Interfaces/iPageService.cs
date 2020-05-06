using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IPageService
    {
        Task<Page> GetPageBySlug(string slug);

        Task<Page> GetPageObjById(int slug);


        Task<IEnumerable<Pages>> GetAll();

        Task<Pages> GetPageById(int id);

        Task<Containers> GetContainerById(int id);

        Task<Pages> AddDetails(PageAddViewModel model);

        Task<Containers> AddContainer(ContainerAddViewModel model);


        Task<bool> Remove(int id);


        Task<bool> RemoveContainer(int id);


        Task<Pages> UpdateDetails(PageUpdateViewModel model);


        Task<Containers> UpdateContainer(ContainerUpdateViewModel model);

        Task<bool> ArrangeContainers(PageGrid grid);

    }
}
