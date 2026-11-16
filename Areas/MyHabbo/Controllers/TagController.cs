using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    /// <summary>Tags on a user's own profile and on groups they can edit. Replies the JS knows: invalidtag, taglimit, valid.</summary>
    [Area("MyHabbo")]
    public class TagController : HomeEditController
    {
        private const string ListView = "~/Areas/MyHabbo/Views/Tag/List.cshtml";
        private const int MaxTags = 20;

        private readonly ITagService _tagService;

        public TagController(IHomeService homeService, ITagService tagService) : base(homeService)
        {
            _tagService = tagService;
        }

        /// <summary>The tag in lower case, or null if it isn't allowed.</summary>
        private static string CleanTag(string tagName)
        {
            var tag = tagName?.Trim().ToLower();
            return string.IsNullOrEmpty(tag) || tag.Length < 2 || tag.Length > 20 || tag.Contains("fuck") ? null : tag;
        }

        private static bool CanAdd(List<Tags> existing, string tag) => existing.Count < MaxTags && !existing.Exists(s => s.Tag == tag);

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/tag/add")]
        public async Task<IActionResult> Add(int accountId, string tagName)
        {
            if (CurrentUserId is not int userId) return Error();
            var tag = CleanTag(tagName);
            if (tag == null) return Content("invalidtag");

            // You tag yourself, so your own tags count towards the limit.
            if (!CanAdd(await _tagService.TagsForUser(userId, true), tag)) return Content("taglimit");

            await _tagService.AddTag(new Tags { UserId = userId, Tag = tag });
            return Content("valid");
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/tag/addgrouptag")]
        public async Task<IActionResult> AddGroupTag(int groupId, string tagName)
        {
            if (CurrentUserId is not int userId) return Error();
            var tag = CleanTag(tagName);
            if (tag == null || !await HomeService.CanEditHome(groupId, userId)) return Content("invalidtag");

            if (!CanAdd(await _tagService.TagsForGroup(groupId, true), tag)) return Content("taglimit");

            await _tagService.AddTag(new Tags { Tag = tag, GroupId = groupId });
            return Content("valid");
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/tag/remove")]
        public async Task<IActionResult> Remove(int accountId, string tagName)
        {
            if (CurrentUserId is not int userId) return Error();

            if (!string.IsNullOrEmpty(tagName))
            {
                _tagService.RemoveTag(new Tags { UserId = userId, Tag = tagName });
            }
            return View(ListView, await _tagService.TagsForUser(accountId, accountId == userId));
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/tag/removegrouptag")]
        public async Task<IActionResult> RemoveGroupTag(int groupId, string tagName)
        {
            var canEdit = await HomeService.CanEditHome(groupId, CurrentUserId);
            if (canEdit && !string.IsNullOrEmpty(tagName))
            {
                _tagService.RemoveTag(new Tags { GroupId = groupId, Tag = tagName });
            }
            return View(ListView, await _tagService.TagsForGroup(groupId, canEdit));
        }

        /// <summary>Visitors (logged in or not) see the list; its owner gets the edit controls.</summary>
        [Route("myhabbo/tag/list")]
        public async Task<IActionResult> List(string tagMsgCode, int accountId)
        {
            return View(ListView, await _tagService.TagsForUser(accountId, accountId == CurrentUserId));
        }

        [Route("myhabbo/tag/listgrouptags")]
        public async Task<IActionResult> ListGroupTags(string tagMsgCode, int groupId)
        {
            return View(ListView, await _tagService.TagsForGroup(groupId, await HomeService.CanEditHome(groupId, CurrentUserId)));
        }
    }
}
