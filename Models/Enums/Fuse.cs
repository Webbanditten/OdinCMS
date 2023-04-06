using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
namespace KeplerCMS.Models
{
    public enum Fuse
    {
        [Description("housekeeping_pages")]
        housekeeping_pages,
        [Description("housekeeping_website")]
        housekeeping_website,
        [Description("housekeeping_menu")]
        housekeeping_menu,
        [Description("housekeeping")]
        housekeeping,
        [Description("housekeeping_localization")]
        housekeeping_localization,
        [Description("housekeeping_campaign")]
        housekeeping_campaign,
        [Description("housekeeping_upload")]
        housekeeping_upload,
        [Description("housekeeping_news")]
        housekeeping_news,
        [Description("fuse_administrator_access")]
        fuse_administrator_access,
        [Description("housekeeping_ranks")]
        housekeeping_ranks,
        [Description("housekeeping_settings")]
        housekeeping_settings,
        [Description("fuse_kick")]
        fuse_kick,
        [Description("fuse_badges")]
        fuse_badges,
        [Description("fuse_room_alert")]
        room_alert,
        [Description("fuse_ban")]
        fuse_ban,
        [Description("fuse_private_rooms")]
        fuse_private_rooms
        
    }
}
