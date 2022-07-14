using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Services.Interfaces;
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
        public async void SitemapXml()
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
            string host = _configuration.GetSection("keplercms:publicUrl").Value;
            Response.ContentType = "application/xml"; 
            using (var xml = XmlWriter.Create(Response.Body, new XmlWriterSettings { Indent = true }))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
                foreach (var item in data)
                {
                    xml.WriteStartElement("url");
                    xml.WriteElementString("loc", host + item.Loc);
                    xml.WriteElementString("priority",item.Priority);
                    xml.WriteElementString("changefreq",item.ChangeFreq);
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
        }
    }
}
