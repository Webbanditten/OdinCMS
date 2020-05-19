using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.MyHabbo.Models;
using System.Web;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class NoteEditorController : Controller
    {

        private readonly IHomeService _homeService;
        public NoteEditorController(IHomeService homeService)
        {

            _homeService = homeService;
        }
        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Editor(NoteEditorViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Preview(NoteEditorViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Place(NoteEditorViewModel model)
        {
            var item = await _homeService.PlaceNote(model.Skin, model.NoteText, int.Parse(User.Identity.Name));
            Response.Headers.Add("x-json", item.Item.Id.ToString());
            return View("~/Areas/MyHabbo/Views/Items/Stickie.cshtml", item);
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/stickie/edit")]
        public async Task<IActionResult> Edit(int skinId, int stickieId)
        {
            var userId = int.Parse(User.Identity.Name);
            var updatedItem = await _homeService.EditItem(stickieId, skinId, userId);

            Response.Headers.Add("x-json", "{\"id\":\"" + updatedItem.Item.Id + "\",\"cssClass\":\"" + HttpUtility.UrlEncode(updatedItem.Item.Skin) + "\",\"type\":\"stickie\"}");
            return View("~/Areas/MyHabbo/Views/Items/Stickie.cshtml", updatedItem);
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/stickie/delete")]
        public async Task<IActionResult> Delete(int stickieId)
        {
            await _homeService.RemoveItem(stickieId, int.Parse(User.Identity.Name));
            return Content("SUCCESS");
        }
    }

}
