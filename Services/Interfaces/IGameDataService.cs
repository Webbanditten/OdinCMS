using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IGameDataService
    {
        Task<Dictionary<string, string>> GetTexts(IEnumerable<string> keys);
        Task<string> GetVariable(string key, string fallback);
        Task<string> GetImageLibraryUrl();
        Task<List<string>> GetLibraryImages(string folder, string language);
    }
}
