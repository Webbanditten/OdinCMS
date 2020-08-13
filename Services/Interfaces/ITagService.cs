using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ITagService
    {

        public Task<List<Tags>> TagsForUser(int userId);
        public Task<List<Tags>> TagsForGroup(int userId);

        public Task<Tags> AddTag(Tags tag);

        public void RemoveTag(Tags tag);


    }
}
