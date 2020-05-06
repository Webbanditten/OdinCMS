using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace KeplerCMS.Controllers
{
    public class Images : Controller
    {
        private readonly IUploadService _uploadService;

        public Images(IUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [LoggedInFilter]
        public async Task<IActionResult> Index(string category, string fileName)
        {
            var file = await _uploadService.GetByCategoryAndName(category, fileName);
            if(file != null)
            {
                int durationInSeconds = 60 * 60 * 24;
                HttpContext.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + durationInSeconds;
                return File(file.Blob, file.ContentType);
            } else
            {
                return NotFound();
            }
        
        }
    }
}
