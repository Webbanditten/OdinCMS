using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class TagController : Controller
    {

        private readonly IUserService _userService;
        private readonly IHomeService _homeService;
        private readonly ITagService _tagService;
        public TagController(IUserService userService, IHomeService homeService, ITagService tagService)
        {
            _userService = userService;
            _tagService = tagService;
            _homeService = homeService;
        }
        [HttpPost]
        [Route("myhabbo/tag/add")]
        public async Task<IActionResult> Add(int accountId, string tagName)
        {
            /*
               invalidtag, taglimit, valid
            */
            if (tagName.Length <= 1 || tagName.Length > 20 || tagName.Contains("fuck") || tagName.Length == 0)
            {
                return Content("invalidtag");
            }

            var userId = int.Parse(User.Identity.Name);
            var tags = await _tagService.TagsForUser(accountId, (accountId == userId));
            tagName = tagName.ToLower();
            if (tags.Count >= 20 || tags.Exists(s=>s.Tag == tagName) != null) {
                return Content("taglimit");
            }

            var tag = await _tagService.AddTag(new Data.Models.Tags { UserId = userId, Tag = tagName.ToLower() });

            return Content("valid");
        }

        [HttpPost]
        [Route("myhabbo/tag/addgrouptag")]
        public async Task<IActionResult> AddGroupTag(int groupId, string tagName)
        {
            /*
               invalidtag, taglimit, valid
            */
            if (tagName.Length <= 1 || tagName.Length > 20 || tagName.Contains("fuck") || tagName.Length == 0)
            {
                return Content("invalidtag");
            }

            var userId = int.Parse(User.Identity.Name);
            var canEdit = await _homeService.CanEditHome(groupId, userId);
            var tags = await _tagService.TagsForGroup(groupId, canEdit);
            tagName = tagName.ToLower();
            if (tags.Count >= 20 || tags.Exists(s=>s.Tag == tagName)) {
                return Content("taglimit");
            }
            if(canEdit) {
                var tag = await _tagService.AddTag(new Data.Models.Tags { Tag = tagName.ToLower(), GroupId = groupId });
            }

            return Content("valid");
        }

        [HttpPost]
        [Route("myhabbo/tag/remove")]
        public async Task<IActionResult> Remove(int accountId, string tagName)
        {
            var userId = int.Parse(User.Identity.Name);
            _tagService.RemoveTag(new Data.Models.Tags { UserId = userId, Tag = tagName });
            var tags = await _tagService.TagsForUser(accountId, (accountId == userId));
            return View("~/Areas/MyHabbo/Views/Tag/List.cshtml", tags);
        }

        [HttpPost]
        [Route("myhabbo/tag/removegrouptag")]
        public async Task<IActionResult> RemoveGroupTag(int groupId, string tagName)
        {
            var userId = int.Parse(User.Identity.Name);
            var canEdit = await _homeService.CanEditHome(groupId, userId);
            if(canEdit) {
                _tagService.RemoveTag(new Data.Models.Tags { GroupId = groupId, Tag = tagName });
            }
            var tags = await _tagService.TagsForGroup(groupId, canEdit);
            return View("~/Areas/MyHabbo/Views/Tag/List.cshtml", tags);
        }

        [Route("myhabbo/tag/list")]
        public async Task<IActionResult> List(string tagMsgCode, int accountId)
        {
            var userId = int.Parse(User.Identity.Name);
            var tags = await _tagService.TagsForUser(accountId, (accountId == userId));
            return View("~/Areas/MyHabbo/Views/Tag/List.cshtml", tags);
        }

        [Route("myhabbo/tag/listgrouptags")]
        public async Task<IActionResult> ListGroupTags(string tagMsgCode, int groupId)
        {
            var userId = int.Parse(User.Identity.Name);
            var canEdit = await _homeService.CanEditHome(groupId, userId);
            var tags = await _tagService.TagsForGroup(groupId, canEdit);
            return View("~/Areas/MyHabbo/Views/Tag/List.cshtml", tags);
        }
    }

}
