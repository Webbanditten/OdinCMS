using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Web;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class NoteEditorController : HomeEditController
    {
        private const string StickieView = "~/Areas/MyHabbo/Views/Items/Stickie.cshtml";

        public NoteEditorController(IHomeService homeService) : base(homeService)
        {
        }

        [HttpPost]
        [LoggedInFilter]
        public IActionResult Editor(NoteEditorViewModel model)
        {
            return View(model);
        }

        [HttpPost]
        [LoggedInFilter]
        public IActionResult Preview(NoteEditorViewModel model)
        {
            return View(model);
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Place(NoteEditorViewModel model)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            // No note left to write on: the editor offers to buy some (myhabbo-store.js).
            if (await HomeService.GetNoteCount(userId) < 1) return Content("NONOTES");

            var item = await HomeService.PlaceNote(homeId, model.Skin, model.NoteText, userId);
            if (item == null) return Error();

            SetJson(item.Item.Id.ToString());
            return View(StickieView, item);
        }

        /// <summary>The "Køb noter" question: the notes product, bought through the store's purchase_notes.</summary>
        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Purchase(bool none)
        {
            return View("~/Areas/MyHabbo/Views/Store/PurchaseConfirm.cshtml", new PurchaseConfirmViewModel
            {
                Product = await HomeService.GetNotesProduct(),
                Notice = none ? Westwind.Globalization.DbRes.T("note_none", "habbohome") : null,
            });
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/stickie/edit")]
        public async Task<IActionResult> Edit(int skinId, int stickieId)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            var item = await HomeService.EditItem(homeId, stickieId, skinId, userId);
            if (item == null) return Error();

            SetJson("{\"id\":\"" + item.Item.Id + "\",\"cssClass\":\"" + HttpUtility.UrlEncode(item.Item.Skin) + "\",\"type\":\"stickie\"}");
            return View(StickieView, item);
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/stickie/delete")]
        public async Task<IActionResult> Delete(int stickieId)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            return await HomeService.RemoveItem(homeId, stickieId, userId) == null ? Error() : Content("SUCCESS");
        }
    }
}
