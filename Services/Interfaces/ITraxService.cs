using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ITraxService
    {
        Task<List<SoundMachineSongs>> GetSongsByOwner(int ownerId);
        Task<SoundMachineSongs> GetSingleSongById(int id);
        string GetTrack(string data, int track);

    }
}
