using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Services.Interfaces;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace KeplerCMS.Controllers
{
    public class SitemapViewModel
    {      
        public string Loc { get; set; }
        public string Priority { get; set; }
        public string ChangeFreq { get; set; }
        public string LastModified { get; set;}
    }
    public class SitemapController : Controller
    {
        private readonly IPageService _pageService;
        private readonly IConfiguration _configuration;

        public SitemapController(IConfiguration configuration, IPageService pageService)
        {
            _configuration = configuration;
            _pageService = pageService;
        }

        [Route("/sitemap.xml")]
        public async Task<IActionResult> SitemapXml()
        {
            var data = new List<SitemapViewModel>();
            foreach (var page in await _pageService.GetAll())
            {
                var priority = 0.5;
                switch(page.Slug) {
                    case "home":
                        priority = 1;
                        break;
                }
                data.Add(new SitemapViewModel { Loc="/" + page.Slug,Priority=priority.ToString(),ChangeFreq="Weekly"});
            }

            data.Add(new SitemapViewModel { Loc="/news",Priority="1",ChangeFreq="Weekly"});
            string host = _configuration.GetSection("keplercms:publicUrl").Value;

            // Build the document in memory and return it. This used to be `async void` writing straight to
            // Response.Body: the request completed before the DB query finished, the scoped DbContext went back
            // to the pool while still in use (corrupting other requests), and the unobserved exception from the
            // async void could terminate the whole process.
            using var stream = new MemoryStream();
            using (var xml = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
                foreach (var item in data)
                {
                    xml.WriteStartElement("url");
                    xml.WriteElementString("loc", host + item.Loc);
                    xml.WriteElementString("changefreq",item.ChangeFreq);
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            return File(stream.ToArray(), "application/xml");
        }
    }
}
