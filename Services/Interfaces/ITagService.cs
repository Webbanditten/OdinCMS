using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ITagService
    {

        public Task<List<Tags>> TagsForUser(int userId, bool editMode);
        public Task<List<Tags>> TagsForGroup(int groupId, bool editMode);

        public Task<Tags> AddTag(Tags tag);

        public void RemoveTag(Tags tag);

        public Task<List<Tags>> Search(string tag);
        public Task<int> Battle(string tag1, string tag2);


    }
}
