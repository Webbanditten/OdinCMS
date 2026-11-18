using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IAdService
    {
        /// <summary>The ads of a placement that are on and inside their dates.</summary>
        Task<List<Ad>> GetActive(string placement);

        /// <summary>One of the ads, by weight (null for none).</summary>
        Ad PickWeighted(IReadOnlyList<Ad> ads);

        /// <summary>A public room's billboard format (small_left ...), or null if it has no billboard.</summary>
        Task<string> RoomFormat(int roomId);

        /// <summary>An ad image's bytes: a site file (/c_images/..., /images/...) or a media library upload (/images/cat/name).</summary>
        Task<(byte[] Bytes, string ContentType)?> LoadImage(string path);

        /// <summary>A billboard for a room's frame: fit "auto" slanted to the format, a ready-made one as it is; both moved by
        /// the room's dx/dy (PNG, cached).</summary>
        Task<(byte[] Bytes, string ContentType)?> RenderBillboard(Ad ad, string format, int dx, int dy);

        /// <summary>Counted in memory and written to cms_ads every minute (AdCounters).</summary>
        void CountView(int adId);

        void CountClick(int adId);
    }
}
