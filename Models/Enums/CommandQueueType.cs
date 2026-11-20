using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Models.Enums
{
    public enum CommandQueueType
    {
        refresh_appearance,
        update_credits,
        reduce_credits,
        send_friend_request,
        purchase_furni,
        room_forward,
        campaign,
        remote_alert,
        remote_ban,
        remote_kick,
        update_room,
        update_infobus,
        reset_bots,
        reload_catalogue,
        reload_navigator,
        reload_inventory,
        reload_settings,
        reload_ads,
        /// <summary>Odin v1.34.0: Days &gt; 0 gives that many days of Habbo Club, otherwise ends the membership now.</summary>
        club_days
    }
}
