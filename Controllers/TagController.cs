using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using KeplerCMS.Models;
using Westwind.Globalization;
using KeplerCMS.Areas.MyHabbo.Models;
using System.Linq;

namespace KeplerCMS.Controllers
{
    [MenuFilter]
    public class TagController : Controller
    {
        private readonly ITagService _tagService;
        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }


        [Route("tag")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Index()
        {
            return View(new TagSearchViewModel { Cloud = await _tagService.TagCloud(), SearchResult = null });
        }

        [Route("tag/search")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Search(string tag, int pageNumber = 1)
        {
            return View("Index", new TagSearchViewModel { Cloud = await _tagService.TagCloud(), SearchResult = await _tagService.Search(tag), PageNumber = pageNumber});
        }

        [Route("habblet/ajax/tagfight")] 
        [HttpPost]
        public async Task<IActionResult> Fight(string tag1, string tag2) {
            var result = await _tagService.Battle(tag1, tag2);
            return View(result);
        }
    }
}
