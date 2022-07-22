using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Helpers;
using System;
using System.IO;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace KeplerCMS.Controllers
{
    public class PhotoController : Controller
    {
        private readonly IPhotoService _photoService;
        private readonly IConfiguration _configuration;
        public PhotoController(IConfiguration configuration, IPhotoService photoService)
        {
            _configuration = configuration;
            _photoService = photoService;
        }

        [HttpGet("habbo-imaging/photo")]
        public async Task<IActionResult> GetPhoto()
        {
            int photoId = 0;
            string style = "sepia";
            if (Request.Query.ContainsKey("id"))
            {
                Request.Query.TryGetValue("id", out var value);
                photoId = int.Parse(value.ToString());
            }
            if (Request.Query.ContainsKey("style"))
            {
                Request.Query.TryGetValue("style", out var value);
                style = value.ToString();
            }
            if(photoId == 0) return NotFound();
            if(string.IsNullOrEmpty(_configuration.GetSection("keplercms:photoService").Value)) return StatusCode(502);
            
            var photo = await _photoService.Get(photoId);
            if (photo != null)
            {
                HttpWebRequest req = WebRequest.Create(new Uri(_configuration.GetSection("keplercms:photoService").Value + "?style=" + style)) as HttpWebRequest;
                req.KeepAlive = false;
                req.Method = "POST";
                req.ContentType = "stream/octet-stream";
                req.ContentLength = photo.PhotoData.Length;

                using (var reqStream = await req.GetRequestStreamAsync())
                {
                    reqStream.Write(photo.PhotoData);
                }
                HttpWebResponse response = null;

                try
                {
                    
                    response = await req.GetResponseAsync() as HttpWebResponse;
                    // send cache headers to the client
                    Response.Headers.Add("Cache-Control", "public, max-age=31536000");
                    return File(response.GetResponseStream(), "image/png");
                }
                catch (Exception e)
                {
                    throw e;
                }

            }
            return NotFound("PHOTO NOT FOUND");

        }

        [Route("habbo-imaging/photo/wall")]
        public async Task<IActionResult> Photos(int skip = 0)
        {
            var photo = await _photoService.GetMany(skip);
            var content = "<style>* { image-rendering: optimizeSpeed;image-rendering: -moz-crisp-edges;image-rendering: -o-crisp-edges;image-rendering: -webkit-optimize-contrast;image-rendering: pixelated;image-rendering: optimize-contrast;}</style>";
            foreach (var p in photo)
            {
                content += "<img src='/habbo-imaging/photo?id=" + p.Id + "' />";
            }
            return Content(content, "text/html");
        }

    }
}
