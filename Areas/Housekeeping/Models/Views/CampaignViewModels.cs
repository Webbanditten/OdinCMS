using System;
using System.Collections.Generic;
using KeplerCMS.Data.Models;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class CampaignListViewModel
    {
        public List<Campaign> Campaigns { get; set; }
        public string Message { get; set; }
    }

    public class CampaignEditViewModel
    {
        public Campaign Campaign { get; set; }
        public List<CataloguePages> AllCataloguePages { get; set; }
        public List<Rooms> PublicRooms { get; set; }
        public string Message { get; set; }
    }

    public class CampaignCreateModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CampaignUpdateModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class AddActionModel
    {
        public int CampaignId { get; set; }
        public string ActionType { get; set; }
        public string ActionData { get; set; }
    }

    public class EditActionModel
    {
        public int ActionId { get; set; }
        public int CampaignId { get; set; }
        public string ActionData { get; set; }
    }

    public class CloneCampaignModel
    {
        public int CampaignId { get; set; }
        public string NewName { get; set; }
    }
}
