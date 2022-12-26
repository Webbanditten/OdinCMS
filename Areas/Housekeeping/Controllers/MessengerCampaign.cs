using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Services.Interfaces;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class MessengerCampaignController : Controller
    {
        private readonly ICommandQueueService _commandQueueService;

        public MessengerCampaignController(ICommandQueueService commandQueueService)
        {
            _commandQueueService = commandQueueService;
        }

        [HousekeepingFilter(Fuse.housekeeping_campaign)]
        public IActionResult Index(string message = null)
        {
            ViewData["message"] = message;
            return View();
        }

        [HousekeepingFilter(Fuse.housekeeping_campaign)]
        [HttpPost]
        public IActionResult Index(MessengerCampaign model)
        {
            if (ModelState.IsValid)
            {
                _commandQueueService.QueueCommand(KeplerCMS.Models.Enums.CommandQueueType.campaign, new CommandTemplate { OnlineOnly = model.OnlineOnly, Message = model.Text, MessageUrl = model.Url, MessageLink = model.Link });
                return RedirectToAction("Index", "MessengerCampaign", new { message = "Campaign sent" });
            }
            return View(model);
        }
    }
}
