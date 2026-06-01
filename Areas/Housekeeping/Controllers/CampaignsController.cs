using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class CampaignsController : Controller
    {
        private readonly ICampaignService _campaignService;
        private readonly ICampaignExecutor _campaignExecutor;
        private readonly ICatalogueService _catalogueService;
        private readonly IRoomService _roomService;
        private readonly IAuditLogService _auditService;

        public CampaignsController(
            ICampaignService campaignService,
            ICampaignExecutor campaignExecutor,
            ICatalogueService catalogueService,
            IRoomService roomService,
            IAuditLogService auditService)
        {
            _campaignService = campaignService;
            _campaignExecutor = campaignExecutor;
            _catalogueService = catalogueService;
            _roomService = roomService;
            _auditService = auditService;
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        public async Task<IActionResult> Index(string message = null)
        {
            var campaigns = await _campaignService.GetAll();
            var viewModel = new CampaignListViewModel
            {
                Campaigns = campaigns,
                Message = message
            };
            return View(viewModel);
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpPost]
        public async Task<IActionResult> Create(CampaignCreateModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Name is required");
                return View(model);
            }

            var campaign = new Campaign
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CreatedById = int.Parse(HttpContext.User.Identity.Name)
            };

            await _campaignService.Create(campaign);
            return RedirectToAction("Edit", new { id = campaign.Id });
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        public async Task<IActionResult> Edit(int id, string message = null)
        {
            var campaign = await _campaignService.GetById(id);
            if (campaign == null)
                return RedirectToAction("Index", new { message = "Campaign not found" });

            var viewModel = new CampaignEditViewModel
            {
                Campaign = campaign,
                AllCataloguePages = (await _catalogueService.GetCataloguePages()).ToList(),
                PublicRooms = await _roomService.GetPublicRooms(),
                Message = message
            };

            return View(viewModel);
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpPost]
        public async Task<IActionResult> Update(CampaignUpdateModel model)
        {
            var campaign = await _campaignService.GetById(model.Id);
            if (campaign == null)
                return RedirectToAction("Index", new { message = "Campaign not found" });

            if (campaign.StatusEnum == CampaignStatus.Active)
                return RedirectToAction("Edit", new { id = model.Id, message = "Cannot edit an active campaign" });

            campaign.Name = model.Name;
            campaign.Description = model.Description;
            campaign.StartDate = model.StartDate;
            campaign.EndDate = model.EndDate;

            // Update status based on dates
            if (campaign.StartDate.HasValue && campaign.StartDate.Value > DateTime.Now)
            {
                campaign.StatusEnum = CampaignStatus.Scheduled;
            }
            else if (campaign.StatusEnum == CampaignStatus.Scheduled && (!campaign.StartDate.HasValue || campaign.StartDate.Value <= DateTime.Now))
            {
                campaign.StatusEnum = CampaignStatus.Draft;
            }

            await _campaignService.Update(campaign);
            return RedirectToAction("Edit", new { id = model.Id, message = "Campaign updated" });
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpPost]
        public async Task<IActionResult> AddAction(AddActionModel model)
        {
            var campaign = await _campaignService.GetById(model.CampaignId);
            if (campaign == null)
                return RedirectToAction("Index", new { message = "Campaign not found" });

            if (campaign.StatusEnum == CampaignStatus.Active)
                return RedirectToAction("Edit", new { id = model.CampaignId, message = "Cannot modify an active campaign" });

            var maxOrder = campaign.Actions.Any() ? campaign.Actions.Max(a => a.OrderIndex) : 0;

            var action = new CampaignAction
            {
                CampaignId = model.CampaignId,
                ActionType = model.ActionType,
                ActionData = model.ActionData,
                OrderIndex = maxOrder + 1
            };

            await _campaignService.AddAction(action);
            return RedirectToAction("Edit", new { id = model.CampaignId, message = "Action added" });
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpPost]
        public async Task<IActionResult> UpdateAction(EditActionModel model)
        {
            var campaign = await _campaignService.GetById(model.CampaignId);
            if (campaign == null)
                return RedirectToAction("Index", new { message = "Campaign not found" });

            if (campaign.StatusEnum == CampaignStatus.Active)
                return RedirectToAction("Edit", new { id = model.CampaignId, message = "Cannot modify an active campaign" });

            var action = campaign.Actions.FirstOrDefault(a => a.Id == model.ActionId);
            if (action == null)
                return RedirectToAction("Edit", new { id = model.CampaignId, message = "Action not found" });

            action.ActionData = model.ActionData;
            await _campaignService.UpdateAction(action);
            return RedirectToAction("Edit", new { id = model.CampaignId, message = "Action updated" });
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpPost]
        public async Task<IActionResult> RemoveAction(int actionId, int campaignId)
        {
            var campaign = await _campaignService.GetById(campaignId);
            if (campaign?.StatusEnum == CampaignStatus.Active)
                return RedirectToAction("Edit", new { id = campaignId, message = "Cannot modify an active campaign" });

            await _campaignService.RemoveAction(actionId);
            return RedirectToAction("Edit", new { id = campaignId, message = "Action removed" });
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpPost]
        public async Task<IActionResult> Activate(int id)
        {
            var campaign = await _campaignService.GetById(id);
            if (campaign == null)
                return RedirectToAction("Index", new { message = "Campaign not found" });

            if (!campaign.Actions.Any())
                return RedirectToAction("Edit", new { id, message = "Campaign has no actions to activate" });

            await _campaignExecutor.ActivateCampaign(id);
            return RedirectToAction("Edit", new { id, message = "Campaign activated successfully" });
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpPost]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _campaignExecutor.DeactivateCampaign(id);
            return RedirectToAction("Edit", new { id, message = "Campaign deactivated - changes reverted" });
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpPost]
        public async Task<IActionResult> Clone(CloneCampaignModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NewName))
                return RedirectToAction("Index", new { message = "Please provide a name for the cloned campaign" });

            var userId = int.Parse(HttpContext.User.Identity.Name);
            var cloned = await _campaignService.Clone(model.CampaignId, model.NewName, userId);
            if (cloned == null)
                return RedirectToAction("Index", new { message = "Failed to clone campaign" });

            return RedirectToAction("Edit", new { id = cloned.Id, message = "Campaign cloned successfully" });
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _campaignService.Delete(id);
            if (!result)
                return RedirectToAction("Index", new { message = "Cannot delete an active campaign" });

            return RedirectToAction("Index", new { message = "Campaign deleted" });
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpGet]
        public async Task<IActionResult> GetCataloguePages()
        {
            var pages = await _catalogueService.GetCataloguePages();
            return Json(pages.Select(p => new { p.Id, p.Name, p.NameIndex, Visible = p.IndexVisible == 1 }));
        }

        [HousekeepingFilter(Fuse.fuse_campaigns)]
        [HttpGet]
        public async Task<IActionResult> GetPublicRooms()
        {
            var rooms = await _roomService.GetPublicRooms();
            return Json(rooms.Select(r => new { r.Id, r.Name, r.Casts }));
        }
    }
}
