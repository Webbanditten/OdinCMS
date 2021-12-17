using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;

namespace KeplerCMS.Controllers
{
    public class Images : Controller
    {
        private readonly IUploadService _uploadService;
        private readonly IMemoryCache _cache;
        public Images(IMemoryCache cache, IUploadService uploadService)
        {
            _cache = cache;
            _uploadService = uploadService;
        }

        public async Task<IActionResult> Index(string category, string fileName)
        {
            int durationInSeconds = 60 * 60 * 24;
            HttpContext.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + durationInSeconds;

            string key = category + "/" + fileName;
            byte[] cachedFile = null;
            _cache.TryGetValue<byte[]>(key, out cachedFile);

            if (cachedFile == null  || !(cachedFile.Length > 0))
            {
               var file = await _uploadService.GetByCategoryAndName(category, fileName);
               if(file != null)
                {
                    // TODO PERHAPS ADD EXPIRATION FOR FILES SO WE DONT RUN OUT OF RAM ;)
                    _cache.Set<byte[]>(key, file.Blob);
                    return File(file.Blob, file.ContentType);
                } else
                {
                    return NotFound();
                }
            }

            var metadata = await _uploadService.GetByCategoryAndNameNoBlob(category, fileName);
            return File(cachedFile, metadata.ContentType);
           
            
        
        }
    }
}
