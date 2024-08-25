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
        roomForward,
        campaign,
        remote_alert,
        remote_ban,
        remote_kick,
        update_room,
        update_infobus
    }
}
