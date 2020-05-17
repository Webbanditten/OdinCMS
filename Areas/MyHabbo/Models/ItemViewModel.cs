using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public class ItemViewModel
    {
        public bool IsOwner { get; set; }
        public ItemWidgetDataModel WidgetData { get; set; }
        public HomesItems Item { get; set; }
        public HomesItemData Definition { get; set; }   
        public bool EnableEditing { get; set; }
    }

    public class ItemWidgetDataModel
    {
        public Users User { get; set; }
        public List<Rooms> Rooms { get; set; }
        public List<SoundMachineSongs> SongList { get; set; }
        public List<HomesRating> Ratings { get; set; }
        public List<UsersBadges> Badges { get; set; }
        // Badges
        //Guest book
        // Rating
        
    }
}
