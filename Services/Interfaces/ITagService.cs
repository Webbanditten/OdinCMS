using KeplerCMS.Data.Models;
using KeplerCMS.Models;
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

        public Task<List<TagSearchResult>> Search(string tag);
        public Task<TagFightViewModel> Battle(string tag1, string tag2);
        public Task<List<TagCloud>> TagCloud();
    }
}
